using kOS.AddOns.RemoteTech;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using System;
using System.Text;

namespace kOS.Persistence
{

    /// <summary>
    /// Persistence extensions needed to store Harddisks in KSP saves files. Perhaps one day we could use serialization instead
    /// and simplify all of this (and make it unit testable too).
    /// </summary>
    public static class PersistenceExtensions
    {
        private const string FilenameValueString = "filename";
        private const string DirnameValueString = "dirname";

        public static Harddisk ToHardDisk(this ConfigNode configNode)
        {
            var capacity = 10000;
            if (configNode.HasValue("capacity"))
                capacity = int.Parse(configNode.GetValue("capacity"));

            var toReturn = new Harddisk(capacity);

            if (configNode.HasValue("volumeName"))
                toReturn.Name = configNode.GetValue("volumeName");

            toReturn.RootHarddiskDirectory = configNode.ToHarddiskDirectory(toReturn, VolumePath.EMPTY);

            return toReturn;
        }

        private static HarddiskDirectory ToHarddiskDirectory(this ConfigNode configNode, Harddisk harddisk, VolumePath path)
        {
            HarddiskDirectory directory = new HarddiskDirectory(harddisk, path);

            foreach (ConfigNode fileNode in configNode.GetNodes("file"))
            {
                directory.CreateFile(fileNode.GetValue(FilenameValueString), fileNode.ToHarddiskFile(harddisk, directory));
            }

            foreach (ConfigNode dirNode in configNode.GetNodes("directory"))
            {
                string dirName = dirNode.GetValue(DirnameValueString);

                directory.CreateDirectory(dirName, dirNode.ToHarddiskDirectory(harddisk, VolumePath.FromString(dirName, path)));
            }

            return directory;
        }

        public static FileContent ToHarddiskFile(this ConfigNode configNode, Harddisk harddisk, HarddiskDirectory directory)
        {
            return Decode(configNode.GetValue("line"));
        }

        public static ConfigNode ToConfigNode(this Harddisk harddisk, string nodeName)
        {
            var node = harddisk.RootHarddiskDirectory.ToConfigNode(nodeName);
            node.AddValue("capacity", harddisk.Capacity);
            node.AddValue("volumeName", harddisk.Name);

            return node;
        }

        public static ConfigNode ToConfigNode(this HarddiskDirectory directory, string nodeName)
        {
            ConfigNode node = new ConfigNode(nodeName);
            node.AddValue(DirnameValueString, directory.Name);

            foreach (VolumeItem item in directory)
            {
                if (item is HarddiskDirectory)
                {
                    HarddiskDirectory dir = item as HarddiskDirectory;
                    node.AddNode(dir.ToConfigNode("directory"));
                }

                if (item is HarddiskFile)
                {
                    HarddiskFile file = item as HarddiskFile;
                    node.AddNode(file.ToConfigNode("file"));
                }
            }

            return node;
        }

        public static ConfigNode ToConfigNode(this HarddiskFile file, string nodeName)
        {
            var node = new ConfigNode(nodeName);
            node.AddValue(FilenameValueString, file.Name);

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

        private static string EncodeBase64(string input)
        {
            return PersistenceUtilities.EncodeBase64(Encoding.ASCII.GetBytes(input));
        }
    }
}