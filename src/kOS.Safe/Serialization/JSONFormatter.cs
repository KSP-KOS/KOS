using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using kOS.Safe.Persistence;

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

        private object WriteJsonObjects(object value)
        {
            var objects = value as IDictionary<object, object>;
            if (objects != null) {
                var stringKeys = new JsonObject();

                foreach (var entry in objects) {
                    stringKeys[entry.Key.ToString()] = WriteJsonObjects(entry.Value);
                }

                return stringKeys;
            } else if (value is List<object>)
            {
                return (value as List<object>).Select(item => WriteJsonObjects(item)).ToList();
            }

            return value;
        }

        public string Write(Dump value)
        {
            return JsonHelper.FormatJson(SimpleJson.SerializeObject(WriteJsonObjects(value)));
        }

        private Dump ReadJsonObject(JsonObject dictionary)
        {
            var result = new Dump();
            /*
            foreach (var entry in dictionary)
            {
                result[entry.Key] = ReadValue(entry.Value);
            }
            */
            return result;
        }

        private object ReadValue(object read)
        {
            var objects = read as JsonObject;
            if (objects != null)
            {
                return ReadJsonObject(objects);
            } if (read is List<object>) {
                return (read as List<object>).Select(item => ReadValue(item)).ToList();
            } else
            {
                return read;
            }
        }

        public Dump Read(string input)
        {
            return ReadJsonObject(SimpleJson.DeserializeObject<JsonObject>(input));
        }


        /// <summary>
        /// Handles JSON indentation.
        /// </summary>
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
                            sb.Append(FileContent.NewLine);
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.Append(FileContent.NewLine);
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
                            sb.Append(FileContent.NewLine);
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

