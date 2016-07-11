using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Exceptions
{
    public class KOSVolumeOutOfRangeException : KOSException
    {
        private const string TERSE_MSG_FMT = "The {0} volume is out of range and not accessible.";

        public KOSVolumeOutOfRangeException()
            :this("requested")
        {
        }

        public KOSVolumeOutOfRangeException(string name)
            :base(string.Format(TERSE_MSG_FMT, name))
        {
        }

        public override string VerboseMessage
        {
            get { return "You have attempted to access a volume " +
                         "which is currently out of range.  This " +
                         "is usually the result of attempting to " +
                         "access the archive volume while RemoteTech " +
                         "does not show a connection."; }
        }
    }
}
