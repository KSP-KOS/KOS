using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Thrown when you attempt to call a user delegate from an invalid context
    /// where it cannot access the delegate from.
    /// </summary>
    public class KOSInvalidDelegateContext : KOSException
    {
        private const string TERSE_MSG_FMT = "Cannot call this lock or function from {0} when it was declared in {1}.";
        private const string HELP_URL = "http://ksp-kos.github.io/KOS_DOC/language/user_functions.html#functions-and-the-terminal-interpreter";
        
        public KOSInvalidDelegateContext(string currentContextName, string intendedContextName) :
            base(String.Format(TERSE_MSG_FMT, currentContextName, intendedContextName))
        {
        }

        public virtual string VerboseMessage
        {
            get { return base.Message; }
        }

        public virtual string HelpURL
        {
            get { return HELP_URL; }
        }
    }
}
