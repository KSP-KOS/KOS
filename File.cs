using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class File : List<String>
    {
        public String Filename;

        public File(File copy)
        {
            Filename = copy.Filename;
            foreach (String line in copy)
            {
                Add(line);
            }
        }

        public File(String filename)
        {
            this.Filename = filename;
        }

        public File(ConfigNode fileNode)
        {
            Load(fileNode);
        }

        public File Copy()
        {
            File retFile = new File(Filename);
            foreach (String line in this)
            {
                retFile.Add(line);
            }

            return retFile;
        }

        public int GetSize()
        {
            int finalSize = 0;
            foreach (String line in this)
            {
                finalSize += line.Length;
            }

            return finalSize;
        }

        public ConfigNode Save(string nodeName)
        {
            ConfigNode node = new ConfigNode(nodeName);

            node.AddValue("filename", Filename);

            foreach (String s in this)
            {
                node.AddValue("line", EncodeLine(s));
            }

            return node;
        }

        internal void Load(ConfigNode fileNode)
        {
            Filename = fileNode.GetValue("filename");

            foreach (String s in fileNode.GetValues("line"))
            {
                Add(DecodeLine(s));
            }
        }

        public static String EncodeLine(String input)
        {
            return input.Replace("{", "&#123;").Replace("}", "&#125;").Replace(" ", "&#32;");     // Stops universe from imploding
        }

        public static String DecodeLine(String input)
        {
            return input.Replace("&#123;", "{").Replace("&#125;", "}").Replace("&#32;", " ");
        }
        
        public string Serialize()
        {
            string output = "";

            foreach (String s in this)
            {
                output += s + "\n";
            }

            return output;
        }

        public void Deserialize(String input)
        {
            this.Clear();

            foreach (String s in input.Split('\n'))
            {
                Add(s);
            }
        }
    }

    public struct FileInfo
    {
        public string Name;
        public int Size;

        public FileInfo(string Name, int Size)
        {
            this.Name = Name;
            this.Size = Size;
        }
    }
}
