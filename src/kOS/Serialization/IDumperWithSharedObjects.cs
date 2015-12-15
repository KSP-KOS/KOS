using kOS.Safe.Encapsulation;

namespace kOS.Serialization
{
    public interface IDumperWithSharedObjects : IDumper
    {
        SharedObjects Shared { set; }
    }
}

