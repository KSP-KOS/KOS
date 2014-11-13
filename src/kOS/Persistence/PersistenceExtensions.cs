using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using kOS.AddOns.RemoteTech2;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace kOS.Persistence
{
    public static class PersistenceExtensions
    {
        private const string FILENAME_VALUE_STRING = "filename";
        private const string MODIFIED_DATE_VALUE_STRING = "modifiedDate";
        private const string CREATED_DATE_VALUE_STRING = "createdDate";

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

            if (configNode.HasValue(MODIFIED_DATE_VALUE_STRING))
            {
                toReturn.ModifiedDate = Convert.ToDateTime(configNode.GetValue(MODIFIED_DATE_VALUE_STRING));
            }
            else
            {
                toReturn.ModifiedDate = DateTime.MinValue;
            }

            if (configNode.HasValue(CREATED_DATE_VALUE_STRING))
            {
                toReturn.ModifiedDate = Convert.ToDateTime(configNode.GetValue(CREATED_DATE_VALUE_STRING));
            }
            else
            {
                toReturn.CreatedDate = DateTime.MinValue;
            }
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
            node.AddValue(MODIFIED_DATE_VALUE_STRING, programFile.ModifiedDate.ToString("s"));
            node.AddValue(CREATED_DATE_VALUE_STRING, programFile.CreatedDate.ToString("s"));

            if (programFile.Category == FileCategory.KSM)
            {
                node.AddValue("line", EncodeBase64(programFile.BinaryContent));
            }
            else
            {
                if (Config.Instance.UseCompressedPersistence)
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

                return Convert.ToBase64String(compressedStream.ToArray());
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
                    byte[] decodedBuffer = DecodeBase64ToBinary(input);
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
                }
            }
            catch (Exception e)
            {
                Debug.Logger.Log(string.Format("Exception decoding: {0} | {1}", e, e.Message));
            }
        }

        public static bool CheckRange(this Volume volume, Vessel vessel)
        {
            var archive = volume as RemoteTechArchive;
            return archive == null || archive.CheckRange(vessel);
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
