namespace kOS.Safe.Exceptions
{
    public class KOSKeyNotFoundException : KOSException
    {
        private const string MSG = "The given key, ({0}), was not present in the case-{1} collection. Use :HASKEY for checking safety.";

        public KOSKeyNotFoundException(string key, bool caseSensitive) : base(string.Format(MSG, key, caseSensitive ? "sensitive" : "insensitive"))
        {

        }
    }
}
