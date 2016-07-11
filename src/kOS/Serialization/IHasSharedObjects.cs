using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;

namespace kOS.Serialization
{
    /// <summary>
    /// Indicates that a class need an instance of kOS.SharedObjects to function.
    ///
    /// SerializationMgr will provide an instance of SharedObjects during deserialization.
    /// </summary>
    /// <seealso cref="IHasSafeSharedObjects"/>
    public interface IHasSharedObjects
    {
        SharedObjects Shared { set; }
    }
}
