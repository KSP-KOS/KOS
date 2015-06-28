using System;

namespace kOS.Safe.Exceptions
{
    public class KOSBadJumpException : Exception
    {
        public KOSBadJumpException(int destination, string message):
            base( String.Format("Can't jump to instruction {0}.  No opcode there: {1}: {2} ",
                                destination,
                                message,
                                "If you see this message, something is broken about how this program got compiled."))
        {
        }

        public virtual string VerboseMessage
        {
            get { return base.Message; }
        }

        public virtual string HelpURL
        {
            get { return string.Empty; }
        }
    }
}