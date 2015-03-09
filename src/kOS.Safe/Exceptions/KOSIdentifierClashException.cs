using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that KOS attempted to make a new identifer which clashes with an
    /// existing identifier in the namespace.
    /// </summary>
    public class KOSIdentiferClashException: KOSException
    {
        private const string TERSE_MSG_FMT = "An identifier called '{0}' is already present at this scope";

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
                "identifier at the same scope level.  You are allowed to make\n" +
                "a more local identifier that masks a more global one, but\n" +
                "it is now an error to make an identifier that has the same\n" +
                "name as one already at the *same* nesting scope level.\n" +
                "\n" +
                " Your variable '{0}' is already the name of a variable here.\n"+
                "\n" +
                "Note that all identifiers you make explicitlty with SET\n" +
                "end up at the same global scope, and all built-in bound\n" +
                "variables (for example SHIP, or MUN) are also at global scope.\n" +
                "\n" +
                "If you wish to mask the name of a built-in identifier with your\n" +
                "own variable, you must be in a local scope (inside braces) and\n" +
                "make the local variable using an explicit DECLARE statement first.\n";

            return String.Format(VERBOSE_TEXT, identifier);
        }
    }
}
