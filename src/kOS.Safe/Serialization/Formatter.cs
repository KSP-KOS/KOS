using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public interface IFormatWriter
    {
        string Write(IDictionary<object, object> value);
    }

    public interface IFormatReader
    {
        IDictionary<object, object> Read(string input);
    }
}

