namespace kOS.Safe.Exceptions
{
    public class KOSKeyNotFoundException : KOSException
    {
        private const string MSG = "The given key {0} was not present, use CONTAINS for checking safety";

        public KOSKeyNotFoundException(string key) : base(string.Format(MSG, key))
        {

        }
    }
}
