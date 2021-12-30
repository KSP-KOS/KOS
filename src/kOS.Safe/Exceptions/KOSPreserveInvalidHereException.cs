using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHereException describing an attempt to use
    /// the PRESERVE keyword when not in a trigger.
    /// </summary>
    public class KOSPreserveInvalidHereException : KOSCommandInvalidHereException
    {
        public override string HelpURL
        {
            get { return "https://ksp-kos.github.io/KOS_DOC/language/flow.html#preserve"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT;} }

        private const string APPEND_TEXT = "\n" +
            "Because PRESERVE alters the behavior of the\n" +
            "trigger body it's inside of, it doesn't mean\n" +
            "anything when it's not inside a trigger like\n" +
            "WHEN or ON.\n";

        public KOSPreserveInvalidHereException(LineCol location) :
            base(location, "PRESERVE", "not in a trigger body", "in triggers" )
        {
        }
    }
}
