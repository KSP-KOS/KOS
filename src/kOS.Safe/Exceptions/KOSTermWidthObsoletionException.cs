using System;

namespace kOS.Safe.Exceptions
{
    public class KOSTermWidthObsoletionException : KOSObsoletionException
    {

        public KOSTermWidthObsoletionException(string version) :
            base(version, "terminal:CHARWIDTH", "the font choosing its own width based on the CHARHEIGHT","")
        {
        }
    }
}

