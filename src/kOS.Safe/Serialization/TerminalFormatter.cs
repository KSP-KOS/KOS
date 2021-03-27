using System;
using System.Text;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using System.Linq;

namespace kOS.Safe.Serialization
{
    public class TerminalFormatter : IFormatWriter
    {
        private static string INDENT = "  ";
        private static int MAX_DEPTH = 2;
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
            StringBuilder result = new StringBuilder(1024);
            WriteInternal(value, result);
            return result.ToString();
        }

        private void WriteInternal(object o, StringBuilder builder, int level = 0)
        {

            if (o == null)
            {
                builder.Append("<null>");
                return;
            }
            if (o is bool || o is int || o is double || o is float)
            {
                builder.Append(o.ToString());
                return;
            }
            if (o is string)
            {
                builder.Append("\"");
                string[] lines = ((string)o).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                builder.Append(EscapeString(lines[0]));
                foreach (var line in lines.Skip(1))
                {
                    builder.Append(Environment.NewLine);
                    WriteIndentation(builder, level);
                    builder.Append(EscapeString(line));
                }
                builder.Append("\"");
                return;
            }
            if (o is Dump)
            {
                Dump d = (Dump)o;

                if (d is DumpWithHeader)
                {
                    builder.Append(((DumpWithHeader)d).Header);
                }

                Dictionary<object, object> reconstructedDict = d;
                switch (d.KeyType)
                {
                    case DumpKeyType.List:
                        IEnumerable<object> enumerable = d[Dump.Items] as IEnumerable<object>;
                        if (enumerable == null)
                        {
                            builder.Append(" <empty>");
                            return;
                        }

                        if (level > MAX_DEPTH)
                        {
                            builder.Append("<list truncated>");
                            return;
                        }

                        WriteInternalEnumerable(enumerable, builder, level, d.Annotations);
                        return;
                    case DumpKeyType.Value:
                        WriteInternal(d["value"], builder, level);
                        return;
                    case DumpKeyType.KeyValue:
                        var items = d[Dump.Entries] as IEnumerable<object>;
                        if (items == null)
                        {
                            builder.Append(" <null>");
                            return;
                        }
                        var keys = items.Where((item, index) => index % 2 == 0).ToList();
                        var values = items.Where((item, index) => index % 2 == 1).ToList();
                        if (keys.Count != values.Count)
                        {
                            builder.Append(" <broken>");
                            return;
                        }
                        reconstructedDict = new Dictionary<object, object>();
                        for (int i = 0; i < keys.Count; i++)
                            reconstructedDict[keys[i]] = values[i];
                        break;
                }

                if (level > MAX_DEPTH)
                {
                    builder.Append("<truncated>");
                    return;
                }

                foreach(var key in reconstructedDict.Keys)
                {
                    builder.Append(Environment.NewLine);
                    WriteIndentation(builder, level);
                    WriteKey(key, builder);
                    builder.Append(": ");
                    if (d.Annotations.ContainsKey(key)) {
                        WriteAnnotated(reconstructedDict[key], builder, level + 1, d.Annotations[key]);
                    } else {
                        WriteInternal(reconstructedDict[key], builder, level + 1);
                    }

                }
                return;
            }
            if (o is IEnumerable<object>)
            {
                if (level > MAX_DEPTH)
                {
                    builder.Append("List of ");
                    builder.Append(((IEnumerable<object>)o).Count().ToString());
                    builder.Append(" items...");
                    return;
                }
                WriteInternalEnumerable((IEnumerable<object>)o, builder, level);
                return;
            }


            // Fall back to type name
            builder.Append("<");
            builder.Append(o.GetType().Name);
            builder.Append(">");
        }

        private void WriteInternalEnumerable(IEnumerable<object> list, StringBuilder builder, int level, Dictionary<object, string> annotations = null)
        {
            int i = 0;
            foreach (object o in list)
            {
                builder.Append(Environment.NewLine);
                WriteIndentation(builder, level);
                builder.Append("- ");
                if (annotations != null && annotations.ContainsKey(i))
                {
                    WriteAnnotated(o, builder, level + 1, annotations[i]);
                } else {
                    WriteInternal(o, builder, level + 1);
                }
                ++i;
            }
        }

        private string EscapeString(string s)
        {
            return s.
                Replace(Environment.NewLine, "\\n").
                Replace("\"", "\"\"");
        }

        private void WriteIndentation(StringBuilder builder, int level)
        {
            while (level > 0)
            {
                builder.Append(INDENT);
                --level;
            }
        }

        // Keys are constrained to one line
        private void WriteKey(object key, StringBuilder builder)
        {
            if (key is string)
            {
                builder.Append(EscapeString((string)key));
                return;
            }
            StringBuilder recursive = new StringBuilder(128);
            WriteInternal(key, builder, 0);
            string result = recursive.ToString();
            if (!result.Contains(Environment.NewLine))
            {
                builder.Append(result);
                return;
            }

            builder.Append(result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0]);
            builder.Append("... truncated");
        }

        private void WriteAnnotated(object o, StringBuilder builder, int level, string annotation)
        {
            StringBuilder recursive = new StringBuilder(1024);
            WriteInternal(o, recursive, level);
            string[] lines = recursive.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            builder.Append(lines[0]);
            builder.Append(" // ");
            builder.Append(annotation);

            foreach(var line in lines.Skip(1))
            {
                builder.Append(Environment.NewLine);
                builder.Append(line);
            }
        }
    }
}

