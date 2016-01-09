using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace kOS.Safe.Serialization
{
    public class JsonFormatter : IFormatWriter, IFormatReader
    {
        private static readonly JsonFormatter instance;

        public static IFormatReader ReaderInstance
        {
            get 
            {
                return instance; 
            }
        }

        public static IFormatWriter WriterInstance
        {
            get 
            {
                return instance; 
            }
        }

        private JsonFormatter()
        {
        }

        static JsonFormatter()
        {
            instance = new JsonFormatter();
        }

        private object MakeStringDictionaries(object value)
        {
            var objects = value as IDictionary<object, object>;
            if (objects == null) return value;

            var stringKeys = new Dictionary<string, object>();

            foreach (var entry in objects)
            {
                stringKeys[entry.Key.ToString()] = MakeStringDictionaries(entry.Value);
            }

            return stringKeys;
        }

        public string Write(Dump value)
        {
            return JsonHelper.FormatJson(SimpleJson.SerializeObject(MakeStringDictionaries(value)));
        }

        private Dump UnwrapDictionary(IDictionary<string, object> dictionary)
        {
            var result = new Dump();

            foreach (var entry in dictionary)
            {
                result[entry.Key] = Unwrap(entry.Value);
            }

            return result;
        }

        private object Unwrap(object read)
        {
            var objects = read as IDictionary<string, object>;
            if (objects == null)
            {
                return read;
            } else
            {
                return UnwrapDictionary(objects);
            }
        }

        public Dump Read(string input)
        {
            return UnwrapDictionary(SimpleJson.DeserializeObject<Dictionary<string, object>>(input));
        }

        class JsonHelper
        {
            private const string INDENT_STRING = "    ";
            public static string FormatJson(string str)
            {
                var indent = 0;
                var quoted = false;
                var sb = new StringBuilder();
                for (var i = 0; i < str.Length; i++)
                {
                    var ch = str[i];
                    switch (ch)
                    {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                    }
                }
                return sb.ToString();
            }
        }

    }

    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
}

