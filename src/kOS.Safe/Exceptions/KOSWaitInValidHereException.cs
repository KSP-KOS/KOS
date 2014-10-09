namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing an attempt to use
    /// the WAIT keyword when in a trigger.
    /// </summary>
    public class KOSWaitInvalidHereException : KOSCommandInvalidHere
    {
        public override string HelpURL
        {
            get { return "http://ksp-kos.github.io/KOS_DOC/summary_topics/CPU_hardware/index.html#WAIT"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT;} }

        private const string APPEND_TEXT = "\n" + 
            "Because WAIT must wait a minimum of at least one\n" +
            "update tick before it can continue, it cannot\n" +
            "work inside a trigger body, which must complete\n" +
            "its work within one update tick.\n";

        public KOSWaitInvalidHereException() :
            base( "WAIT", "in a trigger body", "outside of triggers" )
        {
        }
    }
}
