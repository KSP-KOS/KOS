using System;
using System.Collections.Generic;
using System.Text;

namespace kOS.UserIO
{
    /// <summary>
    /// Subclass of TerminalUnicodeMapper designed to handle the specifics of
    /// the vt100 terminal control codes.
    /// </summary>
    public class TerminalVT100Mapper : TerminalUnicodeMapper
    {
        public TerminalVT100Mapper(string typeString) : base(typeString)
        {
            TerminalTypeID = TerminalType.XTERM;
        }
        
        /// <summary>
        /// Provide the VT100 specific mappings of input chars, then fallback to the
        /// base class's mapping to see if there's other conversions to do.
        /// </summary>
        /// <param name="inChars"></param>
        /// <returns>input mapped into our internal pretend unicode terminal's codes</returns>
        public override string InputConvert(char[] inChars)
        {
            List<char> outChars = new List<char>();
            
            for (int index = 0 ; index < inChars.Length ; ++index )
            {
                switch (inChars[index])
                {
                    case (char)0x1b: // ESCAPE char.
                        if (inChars[index+1] == '[') // ESC followed by '[' is called the CSI (Control Sequence Initiator) and it's how most VT100 codes start.
                        {
                            int numConsumed;
                            char ch = ConvertVT100InputCSI(inChars,index+2,out numConsumed);
                            if (numConsumed > 0)
                            {
                                outChars.Add(ch);
                                index += (1 + numConsumed); // 1+ is for the '[' char.
                            }
                        }
                        else
                            outChars.Add(inChars[index]); // dummy passthrough.  Send ESC as-is.                            
                        break;
                    case (char)0x7f: // DELETE char.
                        outChars.Add((char)UnicodeCommand.DELETELEFT); // Map to the same as backspace, because Vt100 sends it for the backspace key, annoyingly.
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
            System.Console.WriteLine("eraseme: in ConvertVt100InputCSI.  inChars[offset] = (as integer) " + (int)inChars[offset]);
            char returnChar = '\0'; // dummy until changed.
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
            // The following are technically VT220 codes, not VT100, but I couldn't be bothered making a separate
            // mapper just for them.  (i.e. the proper way would be to make a VT220Mapper that inherits from this VT100Mapper,
            // and implement these only there):
            // These codes look like ESC [ _num_ ~.  For example: PgUp is ESC [ 5 ~
            if (offset + 1 < inChars.Length && inChars[offset + 1] == '~')
            {
                switch (inChars[offset])
                {
                    case '1': returnChar = (char)UnicodeCommand.HOMECURSOR;      numConsumed = 2; break;
                    case '4': returnChar = (char)UnicodeCommand.ENDCURSOR;       numConsumed = 2; break;
                    case '5': returnChar = (char)UnicodeCommand.PAGEUPCURSOR;    numConsumed = 2; break;
                    case '6': returnChar = (char)UnicodeCommand.PAGEDOWNCURSOR;  numConsumed = 2; break;
                    default: numConsumed = 0; break; // Do nothing if it's not a recognized sequence.  Leave the chars to be read normally.
                }
            }

            // There's a lot more keyboard controls that could be added to here to really get everything the keyboard can do.

            return returnChar;
        }
    }
}
