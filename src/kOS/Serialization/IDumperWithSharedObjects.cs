using System;
using kOS.Safe.Encapsulation;

namespace kOS.Serialization
{
    public interface IDumperWithSharedObjects : IDumper
    {
        void SetSharedObjects(SharedObjects sharedObjects);
    }
}

