using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Description of KOSInvalidTargetException.
    /// </summary>
    public class KOSInvalidTargetException : KOSException
    {
        public KOSInvalidTargetException(string msg) : base(msg)
        {
        }
    }
}
