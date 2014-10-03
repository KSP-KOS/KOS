using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// A version of KOSCommandInvalidHere describing an attempt to use
    /// the PRESERVE keyword when not in a trigger.
    /// </summary>
    public class KOSPreserveInvalidHereException : KOSCommandInvalidHere, IKOSException
    {
        public override string HelpURL
        {
            get { return "http://ksp-kos.github.io/KOS_DOC/command/flowControl/index.html#PRESERVE"; }
            set {}
        }

        public override string VerboseMessage { get { return VerbosePrefix + appendText;} set{} }
        
        private string appendText =
            "\n" +
            "Because PRESERVE alters the behavior of the\n" +
            "trigger body it's inside of, it doesn't mean\n" +
            "anything when it's not inside a trigger like\n" +
            "WHEN or ON.\n";
        
        public KOSPreserveInvalidHereException() :
            base( "PRESERVE", "not in a trigger body", "in triggers" )
        {
        }
    }
}
