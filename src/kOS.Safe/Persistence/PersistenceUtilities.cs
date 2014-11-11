using System;
using System.Linq;
using kOS.Safe.Compilation;

namespace kOS.Safe.Persistence
{
    public static class PersistenceUtilities
    {
        /// <summary>
        /// Given the first few bytes of content, decide what the FileCategory
        /// should be, based on what's in the Content.<br/>
        /// This should be called before deciding how to set the content.
        /// </summary>
        /// <param name="firstBytes">At least the first four bytes of the file read in binary form - can be longer if you wish</param>
        /// <returns>The type that should be used to store this file.</returns>
        public static FileCategory IdentifyCategory(byte[] firstBytes)
        {
            var returnCat  = FileCategory.UNKNOWN; // default if none of the conditions pass
            var firstFour = new Byte[4];
            int atMostFour = Math.Min(4,firstBytes.Length);
            Array.Copy(firstBytes,0,firstFour,0,atMostFour);
                        
            if (firstFour.SequenceEqual(CompiledObject.MagicId))
            {
                returnCat = FileCategory.KEXE;
            }
            else
            {
                bool isAscii = firstFour.All(b => b == (byte) '\n' || b == (byte) '\t' || b == (byte) '\r' || (b >= (byte) 32 && b <= (byte) 127));
                if (isAscii)
                    returnCat = FileCategory.ASCII;
            }
            return returnCat;

            // There is not currently an explicit test for KERBOSRIPT versus other types of ASCII.
            // At current, any time you want to test for is Kerboscript, make sure to test for is ASCII too,
            // since nothing causes a file to become type kerboscript yet.
        }

        public static string DecodeLine(string input)
        {
            // This could probably be re-coded into something more algorithmic and less hardcoded,
            // as in "any time there is [ampersand,hash,digits,semicolon], replace with the Unicode
            // char for the digits' number."
            return input
                .Replace("&#123;", "{")
                .Replace("&#125;", "}")
                .Replace("&#32;", " ")
                .Replace("&#92;", @"\") 
                .Replace("&#47;", "/")
                .Replace("&#8;", "\t")
                .Replace("&#10", "\n");
        }

        public static string EncodeLine(string input)
        {
            // This could probably be re-coded into something more algorithmic and less hardcoded,
            // as in "For any of the characters in this blacklist, if the character exists then
            // replace it with [ampersand,hash,unicode_num,semicolon] for the char's Unicode value."
            return input
                .Replace("{", "&#123;")
                .Replace("}", "&#125;")
                .Replace(" ", "&#32;")
                .Replace(@"\", "&#92;") 
                .Replace("//", "&#47;&#47;") // a double slash is also a comment in the persistence file's syntax.
                .Replace("\t", "&#8;") // protect tabs, if there are any
                .Replace("\n", "&#10");     // Stops universe from imploding
        }
    }
}
