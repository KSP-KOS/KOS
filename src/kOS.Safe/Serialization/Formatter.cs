using System;
using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public interface Formatter
    {
        string Write(IDictionary<object, object> value);
        IDictionary<object, object> Read(string input);
    }
}

