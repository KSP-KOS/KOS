using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using kOS.AddOns.RemoteTech;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace kOS.Persistence
{
    public static class PersistenceExtensions
    {
        private const string FILENAME_VALUE_STRING = "filename";

        public static Harddisk ToHardDisk(this ConfigNode configNode)
        {
            var capacity = 10000;
            if (configNode.HasValue("capacity")) capacity = int.Parse(configNode.GetValue("capacity"));

            var toReturn = new Harddisk(capacity);
            
            if (configNode.HasValue("volumeName")) toReturn.Name = configNode.GetValue("volumeName");
            
            foreach (ConfigNode fileNode in configNode.GetNodes("file"))
            {
                toReturn.Add(fileNode.ToProgramFile());
            }
            return toReturn;
        }

        public static ProgramFile ToProgramFile(this ConfigNode configNode)
        {
            var filename = configNode.GetValue(FILENAME_VALUE_STRING);
            var toReturn = new ProgramFile(filename);

            Decode(toReturn, configNode.GetValue("line"));
            return toReturn;
        }

        public static ConfigNode ToConfigNode(this Harddisk harddisk, string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue("capacity", harddisk.Capacity);
            node.AddValue("volumeName", harddisk.Name);

            foreach (ProgramFile file in harddisk.FileList.Values)
            {
                node.AddNode(file.ToConfigNode("file"));
            }
            
            return node;
        }

        public static ConfigNode ToConfigNode(this ProgramFile programFile, string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue(FILENAME_VALUE_STRING, programFile.Filename);

            if (programFile.Category == FileCategory.KSM)
            {
                node.AddValue("line", EncodeBase64(programFile.BinaryContent));
            }
            else
            {
                if (SafeHouse.Config.UseCompressedPersistence)
                {
                    node.AddValue("line", EncodeBase64(programFile.StringContent));
                }
                else
                {
                    node.AddValue("line", PersistenceUtilities.EncodeLine(programFile.StringContent));
                }
            }
            return node;
        }

        private static string EncodeBase64(string input)
        {
            return EncodeBase64(Encoding.ASCII.GetBytes(input));
        }

        private static string EncodeBase64(byte[] input)
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

                SafeHouse.Logger.SuperVerbose("About to store the following Base64 string:\n" + returnValue);

                return returnValue;
            }
        }

        private static void Decode(ProgramFile programFile, string input)
        {
            try
            {
                string decodedString;
                try
                {
                    // base64 encoding

                    // Fix for issue #429.  See comment up in EncodeBase64() method above for an explanation:
                    string massagedInput = input.Replace(',','/');
                    
                    byte[] decodedBuffer = DecodeBase64ToBinary(massagedInput);
                    FileCategory whatKind = PersistenceUtilities.IdentifyCategory(decodedBuffer);
                    if (whatKind == FileCategory.ASCII || whatKind == FileCategory.KERBOSCRIPT)
                    {
                        decodedString = Encoding.ASCII.GetString(decodedBuffer);
                        programFile.StringContent = decodedString;
                    }
                    else
                    {
                        programFile.BinaryContent = decodedBuffer;
                    }
                }
                catch (FormatException)
                {
                    // standard encoding
                    decodedString = PersistenceUtilities.DecodeLine(input);
                    programFile.StringContent = decodedString;
                }
            }
            catch (Exception e)
            {
                SafeHouse.Logger.Log(string.Format("Exception decoding: {0} | {1}", e, e.Message));
            }
        }

        public static bool CheckRange(this Volume volume, Vessel vessel)
        {
            var archive = volume as RemoteTechArchive;
            return archive == null || archive.CheckRange(vessel);
        }

        // Provide a way to check the range limit of the archive without requesting the current volume (which throws an error if not in range)
        public static bool CheckCurrentVolumeRange(this IVolumeManager volumeManager, Vessel vessel)
        {
            var rtManager = volumeManager as RemoteTechVolumeManager;
            if (rtManager == null) return true;
            return rtManager.CheckCurrentVolumeRange(vessel);
        }

        private static byte[] DecodeBase64ToBinary(string input)
        {
            byte[] inputBuffer = Convert.FromBase64String(input);

            using (var inputStream = new MemoryStream(inputBuffer))
            // mono requires an installed zlib library for GZipStream to work :(
            //using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var zipStream = new GZipInputStream(inputStream))
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
