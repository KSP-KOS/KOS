using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown whenever KOS compiler encounters something it does not like.
    /// This is to be distinguished from errors that occur while code is
    /// actually running.  This exception, and exceptions derived from
    /// it, might be handled differently because they are expected to
    /// occur *prior* to actually letting the CPU start executing the
    /// program's opcodes.
    /// </summary>
    public class KOSCompileException : KOSException
    {
        public LineCol Location { get; private set; }

        // In order to make the Message property of an Exception not be read-only, you have
        // to do it this way - make a writable field underneath the get-only property.  This
        // is being done so the message of this exception can be altered later after it was
        // constructed, as the source text information isn't available when the exception
        // is first constructed.  (See AddSourceText() below).
        private string message;

        public override string Message
        {
            get { return message; }
        }

        // Just default the Verbose message to return the terse message:
        public override string VerboseMessage { get { return Message; } }

        // Just nothing by default:
        public override string HelpURL { get { return ""; } }

        public KOSCompileException(Token token, string message)
            : this(new LineCol(token.Line, token.Column), message)
        {
        }

        public KOSCompileException(LineCol location, string message)
        {
            Location = location;

            this.message = message;
        }

        /// <summary>
        /// Skims through the source text looking for the line snippet
        /// where the problem is.  This will prepend the source line
        /// info to the existing message in the exception.
        /// </summary>
        /// <param name="sourceText">Text of the entire file that was compiled</param>
        public void AddSourceText(int startline, string sourceText, string filePath)
        {
            // special case for when the exception cannot show its source line:
            if (Location.Line <= 0 || Location.Column <= 0)
                return;
            // Have to skim through the source text looking for the right line:
            int sourceLine = startline;
            int startIndex = 0;
            int endIndex = sourceText.Length; // endIndex is one past the end, actually.
            for (int i = 0; i < sourceText.Length; ++i)
            {
                if (sourceText[i] == '\n')
                {
                    ++sourceLine;
                    if (sourceLine == Location.Line)
                    {
                        startIndex = i + 1;
                    }
                    else if (sourceLine == Location.Column + 1)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }
            message = string.Format(
                "{0}\n{1}\n{2} line:{3} col:{4} {5}",
                sourceText.Substring(startIndex, (endIndex - startIndex)),
                "^".PadLeft(Location.Column), // put the caret under the right column of the source line
                filePath, Location.Line, Location.Column, message);
        }
    }
}