namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing an attempt to use
    /// the WAIT keyword when in a trigger.
    /// </summary>
    public class KOSOnceInvalidHereException : KOSCommandInvalidHereException
    {
        public override string HelpURL
        {
            get { return "http://ksp-kos.github.io/KOS_DOC/summary_topics/CPU_hardware/index.html#WAIT"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT;} }

        private const string APPEND_TEXT = "\n" + 
            "The ONCE keyword only works inside\n" +
            "a program, not from the interpreter\n" +
            "because the interpreter always\n" +
            "recompiles and re-runs the program\n" +
            "each run.\n";

        public KOSOnceInvalidHereException(int line, int col) :
            base(line, col, "ONCE", "from the terminal interpreter", "inside a program" )
        {
        }
    }
}
