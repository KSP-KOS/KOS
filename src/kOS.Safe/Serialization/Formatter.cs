using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public interface IFormatWriter
    {
        string Write(Dump value);
    }

    public interface IFormatReader
    {
        Dump Read(string input);
    }
}

