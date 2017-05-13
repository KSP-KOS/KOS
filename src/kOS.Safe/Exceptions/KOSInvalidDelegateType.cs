namespace kOS.Safe.Exceptions
{
    public class KOSInvalidDelegateType : KOSException
    {
        private const string MSG = "{0} does not return the right type.  Expected {1}, but got {2} instead.";

        public KOSInvalidDelegateType(string delegateName, string expected, string got) : base(string.Format(MSG, delegateName, expected, got))
        {
        }
    }
}
