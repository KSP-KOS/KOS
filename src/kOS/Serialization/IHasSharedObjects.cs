using kOS.Safe.Encapsulation;

namespace kOS.Serialization
{
    public interface IHasSharedObjects
    {
        SharedObjects Shared { set; }
    }
}

