using kOS.Safe.UserIO;
using System.Collections.Generic;
using System.Text;
using System;

namespace kOS.UserIO
{
    /// <summary>
    /// Subclass of TerminalUnicodeMapper designed to handle the specifics of
    /// the vt100 terminal control codes.
    /// </summary>
    public class TerminalVT100Mapper : TerminalUnicodeMapper
    {
        protected const char ESCAPE_CHARACTER = (char)0x1b;
        protected const char BELL_CHAR = (char)0x07;
        protected const char DELETE_CHARACTER = (char)0x7f;
        private readonly string csi = string.Format("{0}{1}", ESCAPE_CHARACTER, '[');


        public TerminalVT100Mapper(string typeString)
            : base(typeString)
        {
            TerminalTypeID = TerminalType.XTERM;
            AllowNativeUnicodeCommands = false;
        }

        private ExpectNextChar outputExpected = ExpectNextChar.NORMAL;
        private int pendingCol;

        ///<summary>
        /// ESC followed by '[' is called the CSI (Control Sequence Initiator) and it's how most VT100 codes start.
        ///</summary>
        protected string CSI
        {
            get { return csi; }
        }

        /// <summary>
        /// Map the Unicode chars (and the fake control codes we made) on output to what the terminal
        /// wants to see.
        /// Subclasses of this should perform their own manipulations, then fall-through
        /// to this base class implementation at the bottom, to allow chains of
        /// subclasses to all operate on the data.
        /// </summary>
        /// <param name="str">Unicode char</param>
        /// <returns>raw byte stream to send to the terminal</returns>
        public override char[] OutputConvert(string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char t in str)
            {
                switch (outputExpected)
                {
                    case ExpectNextChar.TELEPORTCURSORCOL:
                        pendingCol = t;
                        outputExpected = ExpectNextChar.TELEPORTCURSORROW;
                        break;

                    case ExpectNextChar.TELEPORTCURSORROW:
                        int row = t;
                        // VT100 counts rows and cols starting at 1, not 0, thus the +1's below:
                        sb.AppendFormat("{0}{1};{2}H", csi, (row + 1), (pendingCol + 1));
                        outputExpected = ExpectNextChar.NORMAL;
                        break;

                    default:
                        switch (t)
                        {
                            case (char)UnicodeCommand.TELEPORTCURSOR:
                                outputExpected = ExpectNextChar.TELEPORTCURSORCOL;
                                break;

                            case (char)UnicodeCommand.CLEARSCREEN:
                                sb.AppendFormat("{0}2J{0}H", csi);
                                break;

                            case (char)UnicodeCommand.SCROLLSCREENUPONE:
                                sb.AppendFormat("{0}S", csi);
                                break;

                            case (char)UnicodeCommand.SCROLLSCREENDOWNONE:
                                sb.AppendFormat("{0}T", csi);
                                break;

                            case (char)UnicodeCommand.HOMECURSOR:
                                sb.AppendFormat("{0}H", csi);
                                break;

                            case (char)UnicodeCommand.UPCURSORONE:
                                sb.AppendFormat("{0}A", csi);
                                break;

                            case (char)UnicodeCommand.DOWNCURSORONE:
                                sb.AppendFormat("{0}B", csi);
                                break;

                            case (char)UnicodeCommand.RIGHTCURSORONE:
                                sb.AppendFormat("{0}C", csi);
                                break;

                            case (char)UnicodeCommand.LEFTCURSORONE:
                                sb.AppendFormat("{0}D", csi);
                                break;

                            case (char)UnicodeCommand.DELETELEFT:
                                sb.AppendFormat("{0}K", csi);
                                break;

                            case (char)UnicodeCommand.DELETERIGHT:
                                sb.AppendFormat("{0}1K", csi);
                                break;

                            case (char)UnicodeCommand.BEEP:
                                sb.Append(BELL_CHAR);
                                break;
                                
                            case (char)UnicodeCommand.REVERSESCREENMODE:
                                sb.AppendFormat("{0}?5h", csi);
                                break;
                                
                            case (char)UnicodeCommand.NORMALSCREENMODE:
                                sb.AppendFormat("{0}?5l", csi);
                                break;
                                
                            case (char)UnicodeCommand.VISUALBEEPMODE:
                                // sadly, have to consume and ignore - no vt100 code for this, so it's not supported.
                                break;
                                
                            case (char)UnicodeCommand.AUDIOBEEPMODE:
                                // sadly, have to consume and ignore - no vt100 code for this. so it's not supported.
                                break;
                                
                            default:
                                sb.Append(t); // default passhtrough
                                break;
                        }
                        break;
                }
            }
            return base.OutputConvert(sb.ToString());
        }

        /// <summary>
        /// Provide the VT100 specific mappings of input chars, then fallback to the
        /// base class's mapping to see if there's other conversions to do.
        /// </summary>
        /// <returns>input mapped into our internal pretend Unicode terminal's codes</returns>
        public override string InputConvert(char[] inChars)
        {
            List<char> outChars = new List<char>();

            for (int index = 0; index < inChars.Length; ++index)
            {
                switch (inChars[index])
                {
                    case ESCAPE_CHARACTER:
                        if (index + 1 < inChars.Length && inChars[index + 1] == '[')
                            // ESC followed by '[' is called the CSI (Control Sequence Initiator) and it's how most VT100 codes start.
                        {
                            int numConsumed;
                            char ch = ConvertVT100InputCSI(inChars, index + 2, out numConsumed);
                            if (numConsumed > 0)
                            {
                                outChars.Add(ch);
                                index += (1 + numConsumed); // 1+ is for the '[' char.
                            }
                        }
                        else
                            outChars.Add(inChars[index]); // dummy passthrough.  Send ESC as-is.
                        break;

                    case DELETE_CHARACTER: 
                        outChars.Add((char)UnicodeCommand.DELETELEFT); // Map to the same as backspace, because Vt100 sends it for the backspace key, annoyingly.
                        break;

                    case BELL_CHAR:
                        outChars.Add((char)UnicodeCommand.BEEP);
                        break;

                    default:
                        outChars.Add(inChars[index]); // dummy passthrough
                        break;
                }
            }
            return base.InputConvert(outChars.ToArray()); // See if the base class has any more mappings to do on top of these:
        }

        /// <summary>
        /// Return the Unicode (which might be a UnicodeCommand enum value) from the Vt100 CSI sequence
        /// passed in.
        /// </summary>
        /// <param name="inChars">input containing what could be a CSI initiated sequence</param>
        /// <param name="offset">how far into inChars is the first char after the CSI code (ESC[ or CSI).</param>
        /// <param name="numConsumed">how many characters (starting at offset) got consumed and turned into something else.</param>
        /// <returns>The UnicdeCommand equivalent.  NOTE that if numConsumed is zero, this value shouldn't be used as nothing was actually done.</returns>
        protected char ConvertVT100InputCSI(char[] inChars, int offset, out int numConsumed)
        {
            char returnChar = '\0'; // default until changed.
            numConsumed = 0; // default if all the clauses below get skipped.
            if (offset < inChars.Length)
            {
                switch (inChars[offset])
                {
                    case 'A': returnChar = (char)UnicodeCommand.UPCURSORONE;    numConsumed = 1; break;
                    case 'B': returnChar = (char)UnicodeCommand.DOWNCURSORONE;  numConsumed = 1; break;
                    case 'C': returnChar = (char)UnicodeCommand.RIGHTCURSORONE; numConsumed = 1; break;
                    case 'D': returnChar = (char)UnicodeCommand.LEFTCURSORONE;  numConsumed = 1; break;
                    case 'H': returnChar = (char)UnicodeCommand.HOMECURSOR;     numConsumed = 1; break;
                    case 'F': returnChar = (char)UnicodeCommand.ENDCURSOR;      numConsumed = 1; break;
                    default: numConsumed = 0; break; // Do nothing if it's not a recognized sequence.  Leave the chars to be read normally.
                }
            }
            // The following are technically VT220 codes, not VT100, but I couldn't be bothered making a separate
            // mapper just for them.  (i.e. the proper way would be to make a VT220Mapper that inherits from this VT100Mapper,
            // and implement these only there):
            // These codes look like ESC [ _num_ ~.  For example: PgUp is ESC [ 5 ~
            if (offset + 1 < inChars.Length && inChars[offset + 1] == '~')
            {
                switch (inChars[offset])
                {
                    case '1': returnChar = (char)UnicodeCommand.HOMECURSOR;     numConsumed = 2; break;
                    case '3': returnChar = (char)UnicodeCommand.DELETERIGHT;    numConsumed = 2; break;
                    case '4': returnChar = (char)UnicodeCommand.ENDCURSOR;      numConsumed = 2; break;
                    case '5': returnChar = (char)UnicodeCommand.PAGEUPCURSOR;   numConsumed = 2; break;
                    case '6': returnChar = (char)UnicodeCommand.PAGEDOWNCURSOR; numConsumed = 2; break;
                    default: numConsumed = 0; break; // Do nothing if it's not a recognized sequence.  Leave the chars to be read normally.
                }
            }

            // There's a lot more keyboard controls that could be added to here to really get everything the keyboard can do.

            return returnChar;
        }
    }
}