using System.Collections.Generic;
using System.Linq;

namespace kOS
{
    public class File : List<string>
    {
        public string Filename;

        public File(File copy)
        {
            Filename = copy.Filename;
            foreach (var line in copy)
            {
                Add(line);
            }
        }

        public File(string filename)
        {
            Filename = filename;
        }

        public File(ConfigNode fileNode)
        {
            Load(fileNode);
        }

        public File Copy()
        {
            var retFile = new File(Filename);
            retFile.AddRange(this);

            return retFile;
        }

        public int GetSize()
        {
            return this.Sum(line => line.Length);
        }

        public ConfigNode Save(string nodeName)
        {
            var node = new ConfigNode(nodeName);

            node.AddValue("filename", Filename);

            foreach (var s in this)
            {
                node.AddValue("line", EncodeLine(s));
            }

            return node;
        }

        internal void Load(ConfigNode fileNode)
        {
            Filename = fileNode.GetValue("filename");

            foreach (var s in fileNode.GetValues("line"))
            {
                Add(DecodeLine(s));
            }
        }

        public static string EncodeLine(string input)
        {
            return input.Replace("{", "&#123;").Replace("}", "&#125;").Replace(" ", "&#32;");     // Stops universe from imploding
        }

        public static string DecodeLine(string input)
        {
            return input.Replace("&#123;", "{").Replace("&#125;", "}").Replace("&#32;", " ");
        }
        
        public string Serialize()
        {
            return this.Aggregate("", (current, s) => current + (s + "\n"));
        }

        public void Deserialize(string input)
        {
            Clear();

            foreach (var s in input.Split('\n'))
            {
                Add(s);
            }
        }
    }

    public struct FileInfo
    {
        public string Name;
        public int Size;

        public FileInfo(string name, int size)
        {
            Name = name;
            Size = size;
        }
    }
}
