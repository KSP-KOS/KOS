using kOS.AddOns.RemoteTech;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using System;
using System.Text;

namespace kOS.Persistence
{
    public static class PersistenceExtensions
    {
        private const string FILENAME_VALUE_STRING = "filename";

        public static Harddisk ToHardDisk(this ConfigNode configNode)
        {
            var capacity = 10000;
            if (configNode.HasValue("capacity"))
                capacity = int.Parse(configNode.GetValue("capacity"));

            var toReturn = new Harddisk(capacity);

            if (configNode.HasValue("volumeName"))
                toReturn.Name = configNode.GetValue("volumeName");

            foreach (ConfigNode fileNode in configNode.GetNodes("file"))
            {
                toReturn.Save(fileNode.ToHarddiskFile(toReturn));
            }
            return toReturn;
        }

        public static HarddiskFile ToHarddiskFile(this ConfigNode configNode, Harddisk harddisk)
        {
            var filename = configNode.GetValue(FILENAME_VALUE_STRING);

            FileContent fileContent = Decode(configNode.GetValue("line"));
            harddisk.Save(filename, fileContent);
            return new HarddiskFile(harddisk, filename);
        }

        public static ConfigNode ToConfigNode(this Harddisk harddisk, string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue("capacity", harddisk.Capacity);
            node.AddValue("volumeName", harddisk.Name);

            foreach (VolumeFile volumeFile in harddisk.FileList.Values)
            {
                var file = (HarddiskFile) volumeFile;
                node.AddNode(file.ToConfigNode("file"));
            }

            return node;
        }

        public static ConfigNode ToConfigNode(this HarddiskFile file, string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue(FILENAME_VALUE_STRING, file.Name);

            FileContent content = file.ReadAll();

            if (content.Category == FileCategory.KSM)
            {
                node.AddValue("line", PersistenceUtilities.EncodeBase64(content.Bytes));
            }
            else
            {
                if (SafeHouse.Config.UseCompressedPersistence)
                {
                    node.AddValue("line", EncodeBase64(content.String));
                }
                else
                {
                    node.AddValue("line", PersistenceUtilities.EncodeLine(content.String));
                }
            }
            return node;
        }

        private static FileContent Decode(string input)
        {
            try
            {
                return new FileContent(PersistenceUtilities.DecodeBase64ToBinary(input));
            }
            catch (FormatException)
            {
                // standard encoding
                string decodedString = PersistenceUtilities.DecodeLine(input);
                return new FileContent(decodedString);
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
            if (rtManager == null)
                return true;
            return rtManager.CheckCurrentVolumeRange(vessel);
        }

        private static string EncodeBase64(string input)
        {
            return PersistenceUtilities.EncodeBase64(Encoding.ASCII.GetBytes(input));
        }
    }
}