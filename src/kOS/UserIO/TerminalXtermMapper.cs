using System.Collections.Generic;
using System.Text;
using kOS.Safe.UserIO;

namespace kOS.UserIO
{
    /// <summary>
    /// Subclass of TerminalUnicodeMapper designed to handle the specifics of
    /// the xterm terminal control codes.
    /// <br/>
    /// Note that because the XTERM program was designed explicitly to attempt
    /// to emulate the already popular and existent VT100 hardware terminal, this
    /// class can mostly be just a subclass of the VT100 mapper that lets it
    /// do most of the heavy work.  It's only a separate class to support the
    /// few places where XTERM is more capable than VT100.
    /// </summary>
    public class TerminalXtermMapper : TerminalVT100Mapper
    {
        public TerminalXtermMapper(string typeString) : base(typeString)
        {
            TerminalTypeID = TerminalType.XTERM;
            AllowNativeUnicodeCommands = false;
        }

        private ExpectNextChar outputExpected = ExpectNextChar.NORMAL;
        private int pendingWidth;
        private StringBuilder pendingTitle;

        /// <summary>
        /// Map the Unicode chars (and the fake control codes we made) into what the
        /// terminal wants to see.
        /// Subclasses of this should perform their own manipulations, then fallthrough
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
                    case ExpectNextChar.RESIZEWIDTH:
                        pendingWidth = t;
                        outputExpected = ExpectNextChar.RESIZEHEIGHT;
                        break;
                    case ExpectNextChar.RESIZEHEIGHT:
                        int height = t;
                        sb.AppendFormat("{0}8;{1};{2}t", CSI, height, pendingWidth);
                        outputExpected = ExpectNextChar.NORMAL;
                        break;
                    case ExpectNextChar.INTITLE:
                        if (t == (char)UnicodeCommand.TITLEEND)
                        {

                            sb.AppendFormat("{0}]2;{1}{2}", ESCAPE_CHARACTER , pendingTitle, BELL_CHAR);
                            pendingTitle = new StringBuilder();
                            outputExpected = ExpectNextChar.NORMAL;
                        }
                        else
                            pendingTitle.Append(t);
                        break;
                    default:
                        switch (t)
                        {
                            case (char)UnicodeCommand.RESIZESCREEN:
                                outputExpected = ExpectNextChar.RESIZEWIDTH;
                                break;
                            case (char)UnicodeCommand.TITLEBEGIN:
                                outputExpected = ExpectNextChar.INTITLE;
                                pendingTitle = new StringBuilder();
                                break;
                            case (char)UnicodeCommand.CLEARSCREEN:
                                sb.AppendFormat("{0}?47h", ESCAPE_CHARACTER);                   // <-- Tells xterm to use fixed-buffer mode, not saving in scrollback.
                                sb.AppendFormat("{0}2J{0}H", CSI);  // <-- The normal clear screen char from vt100.
                                break;
                            case (char)UnicodeCommand.SHOWCURSOR:
                                sb.AppendFormat("{0}?25h", CSI);
                                break;
                            case (char)UnicodeCommand.HIDECURSOR:
                                sb.AppendFormat("{0}?25l", CSI);
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
        /// Provide the XTERM specific mappings of input chars, then fallback to the
        /// base VT100 mappings for most of its work.
        /// </summary>
        /// <param name="inChars"></param>
        /// <returns>input mapped into our internal pretend Unicode terminal's codes</returns>
        public override string InputConvert(char[] inChars)
        {
            List<char> outChars = new List<char>();
            
            for (int index = 0 ; index < inChars.Length ; ++index )
            {
                switch (inChars[index])
                {
                    case (char)0x9b:
                        // 0x9b is a single 8-bit char alternative to VT100's CSI pair of chars, ESC followed by [.
                        // (In fact DEC referred to 0x9b AS the "CSI character".)
                        // This started appearing on Vt220 models, and xterm borrowed it, as a
                        // more compact way to encode the control sequences.  When you see 0x9b, it means the same
                        // exact thing as ESC [.
                        int numConsumed;
                        char ch = ConvertVT100InputCSI(inChars,index+1,out numConsumed);
                        if (numConsumed > 0)
                        {
                            outChars.Add(ch);
                            index += numConsumed;
                        }
                        break;
                    case (char)(UnicodeCommand.RESIZESCREEN):
                        outChars.Add(inChars[index]); // dummy passthrough - just trapped this to notice the new resize direction.
                        break;
                    default:
                        outChars.Add(inChars[index]); // dummy passthrough
                        break;
                }
            }            
            return base.InputConvert(outChars.ToArray()); // See if the base class has any more mappings to do on top of these:
        }

    }
}
