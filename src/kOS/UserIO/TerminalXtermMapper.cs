using System;
using System.Collections.Generic;
using System.Text;

namespace kOS.UserIO
{
    /// <summary>
    /// Subclass of TerminalUnicodeMapper designed to handle the specifics of
    /// the xterm terminal control codes.
    /// <br/>
    /// Note that because the XTERM program was designed explicitly to attempt
    /// to emulate the already popular and existant VT100 hardware terminal, this
    /// class can mostly be just a subclass of the VT100 mapper that lets it
    /// do most of the heavy work.  It's only a separate class to support the
    /// few places where XTERM is more capable than VT100.
    /// </summary>
    public class TerminalXtermMapper : TerminalVT100Mapper
    {
        public TerminalXtermMapper(string typeString) : base(typeString)
        {
            TerminalTypeID = TerminalType.XTERM;
        }

        private ExpectNextChar outputExpected = ExpectNextChar.NORMAL;
        private int pendingWidth;
        private StringBuilder pendingTitle;

        private enum CommDirection { TO_CLIENT, FROM_CLIENT }

        // These flags exist to stop the following infinite back-and-forth looping that
        // was occurring:
        //     The client sends a resize message to the server, causing the ScreenBuffer to SetSize() itself.
        //     The in-game GUI terminal notices that its ScreenBuffer did a SetSize().
        //     So the in-game GUI terminal sends a resize message to the client.
        //     Although the client's new size is the same as its old, it still goes through the motions of sending its new size notification to the server.
        //     Thus starting the whole thing over again.
        // When doing an active drag, where the client tries to redraw itself the whole time, it got messy.
        private CommDirection prevResizeDirection = CommDirection.TO_CLIENT;
        private DateTime prevResizeTimestamp = DateTime.Now;
        private TimeSpan resizeSanityCooldown = TimeSpan.FromMilliseconds((double)250);
        private bool IsResizeSane(CommDirection resizeDirection)
        {
            return (prevResizeDirection == resizeDirection ||
                    DateTime.Now > prevResizeTimestamp + resizeSanityCooldown);
        }
        private void SetResizeTimestamp(CommDirection resizeDirection)
        {
            prevResizeDirection = resizeDirection;
            prevResizeTimestamp = DateTime.Now;
        }

        /// <summary>
        /// Map the unicode chars (and the fake control codes we made) into bytes.
        /// In this base class, all it does is just mostly passthru things as-is with no
        /// conversions.<br/>
        /// Subclasses of this should perform their own manipulations, then fallthrough
        /// to this base class inmplementation at the bottom, to allow chains of
        /// subclasses to all operate on the data.
        /// </summary>
        /// <param name="ch">unicode char</param>
        /// <returns>raw byte stream to send to the terminal</returns>
        public override char[] OutputConvert(string str)
        {
            System.Console.WriteLine("eraseme: TerminalXtermMapper: Passed string = "+str);
            StringBuilder sb = new StringBuilder();

            for (int index = 0 ; index < str.Length ; ++index)
            {
                switch (outputExpected)
                {
                    case ExpectNextChar.RESIZEWIDTH:
                        pendingWidth = (int)(str[index]);
                        outputExpected = ExpectNextChar.RESIZEHEIGHT;
                        System.Console.WriteLine("eraseme: TerminalXtermMapper: In RESIZEWIDTH mode, just read value = " + pendingWidth);
                        break;
                    case ExpectNextChar.RESIZEHEIGHT:
                        int height = (int)(str[index]);
                        System.Console.WriteLine("eraseme: TerminalXtermMapper: In RESIZEHEIGHT mode, just read value = " + height);
                        if (IsResizeSane(CommDirection.TO_CLIENT))
                        {
                            System.Console.WriteLine("eraseme: TerminalXtermMapper: In RESIZEHEIGHT mode, just read value = " + height + " and decided it's sane to send it.");
                            sb.Append(((char)0x1b)/*ESC*/ + "[8;" + height + ";" + pendingWidth + "t");
                        }
                        // else ignore it and don't pass it on.
                        else
                        {
                            System.Console.WriteLine("eraseme: TerminalXtermMapper: In RESIZEHEIGHT mode, just read value = " + height + " and decided NOT to send it.");
                        }
                        // But whether ignoring it or not, still do remember the attempt was made and reset the timestamp:
                        SetResizeTimestamp(CommDirection.TO_CLIENT);
                        outputExpected = ExpectNextChar.NORMAL;
                        break;
                    case ExpectNextChar.INTITLE:
                        if (str[index] == (char)UnicodeCommand.TITLEEND)
                        {
                            System.Console.WriteLine("eraseme: TerminalXtermMapper: Saw TITLEEND, title = " + pendingTitle);
                            sb.Append(((char)0x1b)/*ESC*/ + "]2;" + pendingTitle.ToString() + ((char)0x07)/*BEL*/);
                            pendingTitle = new StringBuilder();
                            outputExpected = ExpectNextChar.NORMAL;
                            SetResizeTimestamp(CommDirection.TO_CLIENT);
                        }
                        else
                            pendingTitle.Append(str[index]);
                        break;
                    default:
                        switch (str[index])
                        {
                            case (char)(UnicodeCommand.RESIZESCREEN):
                                outputExpected = ExpectNextChar.RESIZEWIDTH;
                                System.Console.WriteLine("eraseme: TerminalXtermMapper: Detected RESIZESCREEN, going into RESIZEWIDTH mode.");
                                break;
                            case (char)(UnicodeCommand.TITLEBEGIN):
                                outputExpected = ExpectNextChar.INTITLE;
                                pendingTitle = new StringBuilder();
                                System.Console.WriteLine("eraseme: TerminalXtermMapper: Detected TITLEBEGIN, going into INTITLE mode.");
                                break;
                            default: 
                                sb.Append(str[index]); // default passhtrough
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
        /// <returns>input mapped into our internal pretend unicode terminal's codes</returns>
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
                        SetResizeTimestamp(CommDirection.FROM_CLIENT);
                        System.Console.WriteLine("eraseme: TerminalXtermMapper: Detected INPUT's RESIZESCREEN, passing thru");
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
