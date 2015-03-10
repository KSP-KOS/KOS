using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that KOS attempted to make a new identifer which clashes with an
    /// existing identifier in the namespace.
    /// </summary>
    public class KOSIdentiferClashException: KOSException
    {
        private const string TERSE_MSG_FMT = "A built-in language identifier called '{0}' is already present at this scope";

        public override string VerboseMessage { get{ return BuildVerboseMessage(); } }

        public override string HelpURL { get{ return "";} }
        
        private string identifier;
        
        public KOSIdentiferClashException(string identifier) :
            base(String.Format(TERSE_MSG_FMT, identifier.TrimStart('$')))
        {
            this.identifier = identifier.TrimStart('$');
        }
        
        private string BuildVerboseMessage()
        {
            const string VERBOSE_TEXT =
                "As of version 0.17, kOS no longer allows you to declare\n" +
                "an identifier that clashes with the name of an existing\n" +
                "built-in bound identifier at the same scope level.  You\n" +
                "are, however, allowed to replace your own homemade variables\n" +
                "(i.e. declare the same variable twice, so the second one\n" +
                "replaces the first).\n" +
                "\n" +
                " Your variable '{0}' is already the name of a built-in variable.\n"+
                "\n" +
                "If you wish to mask the name of a built-in identifier with your\n" +
                "own variable, you must be in a local scope (inside braces) and\n" +
                "make the local variable using an explicit DECLARE statement first.\n";

            return String.Format(VERBOSE_TEXT, identifier);
        }
    }
}
