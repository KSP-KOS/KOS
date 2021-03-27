using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using System.Linq;

namespace kOS.Safe.Serialization
{
    public class TerminalFormatter : IFormatWriter
    {
        public static int INDENT_SPACES = 2;

        // eraseme - MUST INCREASE THIS VALUE AFTER TESTING IS OVER!!!
        public static int MAX_INDENT_LEVEL = 5; // SET LOW DURING TESTING SO IT'S EASY TO TRIGGER IT

        // eraseme - THIS ENTIRE CLASS BELOW THIS POINT IS PROBABLY NOT NEEDED ANYMORE IF THIS PR IS USED.
        // eraseme - IT ONLY USES THE ABOVE TWO SETTINGS (TEST THIS BY REMOVING THE REST OF THIS AND
        // eraseme - SEEING IF IT COMPILES.)
        // eraseme - THE ENTIRE CLASS COULD GO AWAY AND THESE SETTINGS COULD BE MOVED ELSEWHERE,
        // eraseme - WHERE A USER SCRIPT COULD ALTER THEM.

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
            IDictionary<object, object> printedDump;

            if (dump.Count == 1 && dump.ContainsKey(Dump.Items)) {
                // special handling for enumerables
                List<object> list = dump[Dump.Items] as List<object>;
                printedDump = list.Select((x, i) => new { Item = x, Index = (object)i })
                    .ToDictionary(x => x.Index, x => x.Item);
            } else if (dump.Count == 1 && dump.ContainsKey(Dump.Entries)) {
                // special handling for lexicons
                List<object> list = dump[Dump.Entries] as List<object>;

                printedDump = new Dictionary<object, object>();

                for (int i = 0; 2 * i < list.Count; i++)
                {
                    printedDump[list[2 * i]] = list[2 * i + 1];
                }
            } else {
                printedDump = dump;
            }

            return WriteIndentedDump(printedDump, level);
        }

        public string WriteIndentedDump(IDictionary<object, object> dump, int level)
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
                } else if (entry.Key is Dump)
                {
                    string header = Environment.NewLine;

                    var withHeader = entry.Key as DumpWithHeader;
                    if (withHeader != null)
                    {
                        header = withHeader.Header + Environment.NewLine;
                    }

                    string keyString = header + WriteIndented(entry.Key as Dump, level + 1);
                    line += string.Format("[{0}] = ", keyString);
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

