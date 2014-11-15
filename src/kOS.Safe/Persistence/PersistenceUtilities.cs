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
            var firstFour = new Byte[4];
            int atMostFour = Math.Min(4,firstBytes.Length);
            Array.Copy(firstBytes,0,firstFour,0,atMostFour);

            var returnCat = (atMostFour < 4) ? FileCategory.TOOSHORT : FileCategory.OTHER; // default if none of the conditions pass
                        
            if (firstFour.SequenceEqual(CompiledObject.MagicId))
            {
                returnCat = FileCategory.KSM;
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
        
        /// <summary>
        /// This is both for error checking and blessing of user-created filenames,
        /// and to tack on a filename extension if there is none present.
        /// <br/><br/><br/>
        /// Returns a version of the filename in which it has had the file extension
        /// added unless the filename already has any sort of file extension, in
        /// which case nothing is changed about it.  If every place where the auto-
        /// extension-appending is attempted is done via this method, then it will never end
        /// up adding an extension when an explicit one exists already.
        /// </summary>
        /// <param name="fileName">Filename to maybe change.  Can be full path or just the filename.</param>
        /// <param name="extensionName">Extension to add if there is none already.</param>
        /// <param name="trusted">True if the filename is internally generated (and therefore allowed to
        ///   have paths in it).  False if the filename is from a user-land string (and therefore allowing
        ///   a name that walks the directry tree is a security hole.)</param>
        /// <returns></returns>
        public static string CookedFilename(string fileName, string extensionName, bool trusted = false)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new KOSFileException("Attempted to use an empty filename.");
                                           
            int lastDotIndex = fileName.LastIndexOf('.');
            int lastSlashIndex = fileName.LastIndexOfAny( new char[] {'/','\\'} ); // both kinds of OS folder separator.
            
            if (!trusted)
            {
                // Later if we add user folder abilities, this may have to get more fancy about what is
                // and isn't allowed:
                if (fileName.Contains(".."))
                    throw new KOSFileException("kOS does not allow using consecutive dots ('..') in a filename.");
                if (lastSlashIndex >= 0)
                    throw new KOSFileException("kOS does not allow pathname characters ('/','\\') in a filename.");
            }

            if (lastSlashIndex == fileName.Length - 1)
                throw new KOSFileException("Attempted to use a filename consisting only of directory paths");
            
            if (lastDotIndex == lastSlashIndex + 1) // lastSlashIndex == -1 if no slashes so this also covers just passing in ".foo".
                throw new KOSFileException("Attempted to use a filename beginning with a period ('.') character.");
                        
            if (lastDotIndex < 0 || lastDotIndex < lastSlashIndex) // If no dot in the tail part of the filename after any potential directory separators.
                return fileName + "." + extensionName;
            else if (lastDotIndex == fileName.Length - 1) // There is a dot, but it's at the very last character, as in "myfile."
                return fileName + extensionName;

            return fileName;
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
