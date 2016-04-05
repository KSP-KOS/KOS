namespace kOS.Safe.Exceptions
{
    public class KOSAtmosphereDeprecationException : KOSDeprecationException
    {
        public KOSAtmosphereDeprecationException(string version, string oldUsage, string newUsage) : base(version, oldUsage, newUsage)
        {
        }

        public override string VerboseMessage
        {
            get
            {
                return string.Format(
                    "{0}\n" + 
                    "In KSP 1.0 there have been many changes to\n" + 
                    "how planetary atmospheres work.\n" +
                    "\n" + 
                    "Because the basic concepts of much of that system\n" + 
                    "have changed. We have removed TERMVELOCITY and SCALE\n" 
                    , base.Message);
            }
        }
    }
}