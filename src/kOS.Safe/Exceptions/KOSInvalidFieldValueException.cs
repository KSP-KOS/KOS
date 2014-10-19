namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing an attempt to use
    /// the BREAK command when not in the body of a loop.
    /// </summary>
    public class KOSInvalidFieldValueException : KOSException
    {
        public override string HelpURL
        {
            get { return "TODO - need to write docs about this feature and put URL here"; }
        }

        public override string VerboseMessage
        {
            get
            {
                return
                    "kOS tries to only allow scripts to alter the\n" +
                    "fields of other KSP modules when the new value\n" +
                    "fits the user interface established by that module's\n" +
                    "control panel controls.  If a script tries to assign\n" +
                    "a value that would be impossible to assign using the\n" +
                    "GUI control panel for the part, kOS will refuse the\n" +
                    "attempt.  If kOS did not adhere to this rule it might\n" +
                    "allow values to be altered in ways that other mods\n" +
                    "are not built to expect and respond to properly.\n";
            }
        }

        public KOSInvalidFieldValueException(string msg) :
            base(msg)
        {
        }
    }
}