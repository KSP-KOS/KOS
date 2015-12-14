using System.Collections.Generic;

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

        public string Write(IDictionary<object, object> value)
        {
            return SimpleJson.SerializeObject(MakeStringDictionaries(value));
        }
            
        private object Unwrap(object read)
        {
            var objects = read as IDictionary<string, object>;
            if (objects == null) return read;

            var result = new Dictionary<object, object>();

            foreach (var entry in objects)
            {
                result[entry.Key] = Unwrap(entry.Value);
            }

            return result;
        }

        public IDictionary<object, object> Read(string input)
        {
            return (IDictionary<object, object>)Unwrap(SimpleJson.DeserializeObject<Dictionary<string, object>>(input));
        }

    }
}

