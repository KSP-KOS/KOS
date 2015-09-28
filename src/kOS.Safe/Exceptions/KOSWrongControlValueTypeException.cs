using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// To be thrown whenever the script attempted to perform a
    /// cooked control "LOCK", or a raw control "SET", to a value<br/>
    /// that's of an unusable type for that particular control bound variable.
    /// <br/>
    /// Examples:<br/>
    /// - attempting to LOCK THROTTLE TO a string, or a LIST.<br/>
    /// - attempting to SET SHIP:CONTROL:PITCH to a string, or a RGBA color.<br/>
    /// </summary>
    public class KOSWrongControlValueTypeException : KOSException
    {
        public KOSWrongControlValueTypeException(string controlType, string didUse, string shouldUse) :
            base( string.Format("Cannot use a {0} as the value for the {1}.  Should use {2} instead.",
                                didUse, controlType, shouldUse))
        {
        }

        public override string VerboseMessage
        {
            get { return Message + "\n" +
                         "When setting a special control variable\n" +
                         "the type of the value has to match what\n" +
                         "the kOS computer expects that value to be."; }
        }

        public override string HelpURL
        {
            get { return string.Empty; }
        }
    }
}

