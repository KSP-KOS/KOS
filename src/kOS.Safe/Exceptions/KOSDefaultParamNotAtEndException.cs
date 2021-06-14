using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing a situation where the
    /// function parameter list intermingled defaultable optional parameters
    /// with undefaultable mandatory ones.  All the mandatory ones must come
    /// prior to all the optional ones.
    /// </summary>
    public class KOSDefaultParamNotAtEndException : KOSCommandInvalidHereException
    {
        public override string HelpURL
        {
            get { return "https://ksp-kos.github.io/KOS_DOC/language/variables/index.html#DECLARE_PARAMETER"; }
        }

        public override string VerboseMessage { get { return VerbosePrefix + APPEND_TEXT;} }

        private const string APPEND_TEXT = "\n" + 
            "All defaultable parameters must come at the\n" +
            "end of the list of all parameters for a\n" +
            "program or a function.\n" +
            "For example, this is legal:\n" +
            "   declare x, y is 0.\n" +
            "but this is not:\n" +
            "   declare x is 0, y.\n" +
            "because when x had a default then all other\n" +
            "parameters that came after it had to have one.\n";

        public KOSDefaultParamNotAtEndException(LineCol location) :
            base(location,
                 "An optional parameter (one with a default initializer)",
                 "before a mandatory parameter (one without a default initializer)",
                 "when all mandatory parameters come before all optional parameters")
        {
        }
    }
}