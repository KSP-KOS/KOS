using System;
using System.Collections.Generic;

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

        public string Write(IDictionary<object, object> value)
        {
            string header = "";

            var withHeader = value as DictionaryWithHeader;
            if (withHeader != null)
            {
                header = withHeader.Header + Environment.NewLine;
            }

            return header + WriteIndented(value);
        }

        public string WriteIndented(IDictionary<object, object> collection, int level = 0)
        {
            var result = new List<string>();

            foreach (KeyValuePair<object, object> entry in collection)
            {
                var line = string.Empty.PadLeft(level * INDENT_SPACES);
                var value = entry.Value;
                string valueString;

                var objects = value as IDictionary<object, object>;
                if (objects != null)
                {
                    string header = Environment.NewLine;

                    var withHeader = value as DictionaryWithHeader;
                    if (withHeader != null)
                    {
                        header = withHeader.Header + Environment.NewLine;
                    }

                    valueString = header + WriteIndented(objects, level + 1);
                } else
                {
                    valueString = value.ToString();
                }

                if (entry.Key is string)
                {
                    line += string.Format("[\"{0}\"]= {1}", entry.Key, valueString);
                } else
                {
                    line += string.Format("[{0}]= {1}", entry.Key, valueString);
                }
                result.Add(line);
            }

            return String.Join(Environment.NewLine, result.ToArray());
        }
    }
}

