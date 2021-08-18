namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when you attempt to call a user delegate from an invalid context
    /// where it cannot access the delegate from.
    /// </summary>
    public class KOSInvalidDelegateContextException : KOSException
    {
        private const string TERSE_MSG_FMT = "Cannot call this lock or function or delgate from {0} when it was declared in {1}.";
        private const string HELP_URL = "https://ksp-kos.github.io/KOS_DOC/language/user_functions.html#functions-and-the-terminal-interpreter";

        public KOSInvalidDelegateContextException(string currentContextName, string intendedContextName) :
            base(string.Format(TERSE_MSG_FMT, currentContextName, intendedContextName))
        {
        }

        public override string VerboseMessage
        {
            get { return Message; }
        }

        public override string HelpURL
        {
            get { return HELP_URL; }
        }
    }
}