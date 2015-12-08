using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Serialization
{
    public class TerminalFormatter : Formatter
    {
        private static int INDENT_SPACES = 2;
        private static readonly TerminalFormatter instance = new TerminalFormatter();

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

        public string Write(IDictionary<object, object> value)
        {
            string header = "";

            if (value is DictionaryWithHeader)
            {
                header = (value as DictionaryWithHeader).Header + Environment.NewLine;
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

                if (value is IDictionary<object, object>)
                {
                    string header = Environment.NewLine;

                    if (value is DictionaryWithHeader)
                    {
                        header = (value as DictionaryWithHeader).Header + Environment.NewLine;
                    }

                    valueString = header + WriteIndented(value as IDictionary<object, object>, level + 1);
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

        public IDictionary<object, object> Read(string input)
        {
            throw new NotImplementedException();
        }

    }
}

