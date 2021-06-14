using kOS.Safe.Compilation.KS;

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
            get { return "https://ksp-kos.github.io/KOS_DOC/summary_topics/CPU_hardware/index.html#WAIT"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT;} }

        private const string APPEND_TEXT = "\n" + 
            "The RUN ONCE or RUNONCEPATH concept\n" +
            "does not work from the interpreter\n" +
            "because the interpreter always\n" +
            "recompiles and re-runs the program\n" +
            "each run anyway.  'ONCE' only has meaning\n" +
            "when a program runs another program.\n" ;

        public KOSOnceInvalidHereException(LineCol location) :
            base(location, "RUN ONCE (or RUNONCEPATH)", "from the terminal interpreter", "inside a program" )
        {
        }
    }
}
