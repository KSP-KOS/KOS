namespace kOS.Safe.Exceptions
{
    public class KOSKeyNotFoundException : KOSException
    {
        private const string MSG = "The given key {0} was not present, use CONTAINS for checking safety. This is a case {1} collection";

        public KOSKeyNotFoundException(string key, bool caseSensitive) : base(string.Format(MSG, key, caseSensitive ? "insensitive" : "sensitive"))
        {

        }
    }
}
