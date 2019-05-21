using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace kOS.Safe.Persistence
{
    public static class PersistenceUtilities
    {
        public static readonly byte[] GzipHeader = new byte[] { (byte)(GZipConstants.GZIP_MAGIC >> 8), (byte)(GZipConstants.GZIP_MAGIC & 0xff), (byte)Deflater.DEFLATED, 0 };
        public static bool IsBinary(FileCategory category)
        {
            return category == FileCategory.BINARY || category == FileCategory.KSM;
        }

        /// <summary>
        /// Given the first few bytes of content, decide what the FileCategory
        /// should be, based on what's in the Content.<br/>
        /// This should be called before deciding how to set the content.
        /// </summary>
        /// <param name="firstBytes">At least the first four bytes of the file read in binary form - can be longer if you wish</param>
        /// <returns>The type that should be used to store this file.</returns>
        public static FileCategory IdentifyCategory(byte[] firstBytes)
        {
            var firstFour = new byte[4];
            int atMostFour = System.Math.Min(4, firstBytes.Length);
            Array.Copy(firstBytes, 0, firstFour, 0, atMostFour);
            var returnCat = atMostFour < 4 ? FileCategory.TOOSHORT : FileCategory.BINARY; // default if none of the conditions pass

            if (firstFour.SequenceEqual(CompiledObject.MagicId) || firstFour.SequenceEqual(GzipHeader))
            {
                returnCat = FileCategory.KSM;
            }
            else
            {
                bool isAscii = firstFour.All(b => b == (byte)'\n' || b == (byte)'\t' || b == (byte)'\r' || (b >= (byte)32 && b <= (byte)127));
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
        ///   a name that walks the directory tree is a security hole.)</param>
        /// <returns></returns>
        public static string CookedFilename(string fileName, string extensionName, bool trusted = false)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new KOSFileException("Attempted to use an empty filename.");

            int lastDotIndex = fileName.LastIndexOf('.');
            int lastSlashIndex = fileName.LastIndexOfAny(new[] { '/', '\\' }); // both kinds of OS folder separator.

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
            if (lastDotIndex == fileName.Length - 1) // There is a dot, but it's at the very last character, as in "myfile."
                return fileName + extensionName;

            return fileName;
        }

        public static string DecodeLine(string input)
        {
            StringBuilder output = new StringBuilder();
            for (int inputPos = 0; inputPos < input.Length; ++inputPos)
            {
                char ch = input[inputPos];
                if (ch == '&')
                {
                    // The reason for catching all these exception cases is because people might use this
                    // new code to try to read files that the old buggy code wrote out incorrectly:
                    if (input[inputPos + 1] != '#')
                        throw new KOSPersistenceException("Improperly encoded saved file contains '&' without '#'");
                    int semicolonPos = input.IndexOf(';', inputPos);
                    if (semicolonPos < 0)
                        throw new KOSPersistenceException("Improperly encoded saved file contains '&' without closing ';'");
                    int charOrdinal;
                    if (!int.TryParse(input.Substring(inputPos + 2, semicolonPos - (inputPos + 2)), NumberStyles.Integer, CultureInfo.InvariantCulture, out charOrdinal))
                        throw new KOSPersistenceException("Improperly encoded saved file contains non-digits between the '&#' and the ';'");
                    output.Append((char)charOrdinal);
                    inputPos = semicolonPos; // skip to the end of the encoding section, as if everything between '&' and ';' was one char of input.
                }
                else
                {
                    output.Append(ch);
                }
            }
            return output.ToString();
        }

        public static string EncodeLine(string input)
        {
            StringBuilder output = new StringBuilder();
            foreach (char ch in input)
            {
                if (CharNeedsEncoding(ch))
                    output.Append("&#" + (uint)ch + ";"); // Casting to uint will get the Unicode number of the character
                else
                    output.Append(ch);
            }
            return output.ToString();
        }

        /// <summary>
        /// Returns true if the character has to be encoded and cannot be dumped into a persistence file as-is.
        /// </summary>
        /// <param name="character">character to test</param>
        /// <returns>true if the character needs protective encoding</returns>
        public static bool CharNeedsEncoding(char character)
        {
            return !(char.IsLetterOrDigit(character) || WHITELISTED_SYMBOLS.Contains(character));
        }


        public static string EncodeBase64(byte[] input)
        {
            using (var compressedStream = new MemoryStream())
            {
                // mono requires an installed zlib library for GZipStream to work :(
                // using (Stream csStream = new GZipStream(compressedStream, CompressionMode.Compress))
                using (Stream csStream = new GZipOutputStream(compressedStream))
                {
                    csStream.Write(input, 0, input.Length);
                }

                string returnValue = Convert.ToBase64String(compressedStream.ToArray());

                // Added the following to fix issue #429:  Base64 content can include the slash character '/', and
                // if it happens to have two of them contiguously, it forms a comment in the persistence file and
                // truncates the value.  So change them to a different character to protect the file.
                // The comma ',' char is not used by base64 so it's a safe alternative to use as we'll be able to
                // swap all of the commas back to slashes on reading, knowing that commas can only appear as the
                // result of this swap on writing:
                returnValue = returnValue.Replace('/',',');

                //SafeHouse.Logger.SuperVerbose("About to store the following Base64 string:\n" + returnValue);

                return returnValue;
            }
        }

        public static byte[] DecodeBase64ToBinary(string input)
        {
            // Fix for issue #429.  See comment up in EncodeBase64() method above for an explanation:
            string massagedInput = input.Replace(',','/');

            byte[] inputBuffer = Convert.FromBase64String(massagedInput);

            using (var inputStream = new MemoryStream(inputBuffer))
            {
                // mono requires an installed zlib library for GZipStream to work :(
                //using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var zipStream = new GZipInputStream(inputStream))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        var buffer = new byte[4096];
                        int read;

                        while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            decompressedStream.Write(buffer, 0, read);
                        }

                        return decompressedStream.ToArray();
                    }
                }
            }
        }

        // Note: deliberately missing from the whitelist are:
        //  '&' - because it's the marker used to start an encoding.
        //  '/' - because while one is safe, two of them consecutively would start a comment,
        //          and it's just easier to encode all of them than write the special logic that tracks
        //          the context of prev char or next char.
        private const string WHITELISTED_SYMBOLS = "~`!@#$%^*()_-+=[]|:;\"'<>,.?";
    }
}