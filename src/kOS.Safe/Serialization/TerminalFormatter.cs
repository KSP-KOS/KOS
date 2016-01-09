using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Serialization
{
    public class TerminalFormatter : IFormatWriter
    {
        private static int INDENT_SPACES = 2;
        private static readonly TerminalFormatter instance;

        public static TerminalFormatter Instance
        {
            get 
            {
                return instance;
            }
        }

        private TerminalFormatter()
        {

        }

        static TerminalFormatter()
        {
            instance = new TerminalFormatter();
        }

        public string Write(Dump value)
        {
            string header = "";

            var withHeader = value as DumpWithHeader;
            if (withHeader != null)
            {
                header = withHeader.Header + Environment.NewLine;
            }

            return header + WriteIndented(value);
        }

        public string WriteIndented(Dump dump, int level = 0)
        {
            var result = new List<string>();

            foreach (KeyValuePair<object, object> entry in dump)
            {
                var line = string.Empty.PadLeft(level * INDENT_SPACES);
                var value = entry.Value;
                string valueString;

                var objects = value as Dump;
                if (objects != null)
                {
                    string header = Environment.NewLine;

                    var withHeader = value as DumpWithHeader;
                    if (withHeader != null)
                    {
                        header = withHeader.Header + Environment.NewLine;
                    }

                    valueString = header + WriteIndented(objects, level + 1);
                } else
                {
                    valueString = value.ToString();
                }

                if (entry.Key is string || entry.Key is StringValue)
                {
                    line += string.Format("[\"{0}\"] = ", entry.Key);
                } else
                {
                    line += string.Format("[{0}] = ", entry.Key.ToString());
                }

                if (entry.Value is string)
                {
                    line += string.Format("\"{0}\"", valueString);
                } else
                {
                    line += string.Format("{0}", valueString);
                }

                result.Add(line);
            }

            return String.Join(Environment.NewLine, result.ToArray());
        }
    }
}

