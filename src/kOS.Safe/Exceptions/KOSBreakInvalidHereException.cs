using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing an attempt to use
    /// the BREAK command when not in the body of a loop.
    /// </summary>
    public class KOSBreakInvalidHereException : KOSCommandInvalidHereException
    {
        public override string HelpURL
        {
            get { return "https://ksp-kos.github.io/KOS_DOC/command/flowControl/index.html#BREAK"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT; } }

        private const string APPEND_TEXT = "\n" +
            "Because BREAK causes the current loop to quit\n" +
            "it doesn't mean anything when it's not inside a\n" +
            "loop.\n";

        public KOSBreakInvalidHereException(LineCol location) :
            base(location, "BREAK", "outside a loop", "in a loop body")
        {
        }
    }
}