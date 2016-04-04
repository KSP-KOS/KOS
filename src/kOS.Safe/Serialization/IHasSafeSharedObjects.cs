using System;

namespace kOS.Safe.Serialization
{
    /// <summary>
    /// This exists so that we can keep some of the classes that depend on SharedObjects in kOS.Safe.
    ///
    /// kOS has 2 versions of SharedObjects, one from kOS and one from kOS.Safe. SerializationMgr will automatically supply an instance of
    /// kOS.SharedObjects to any Structures that implements IHasSharedObjects during deserialization. However not all classes that are serializable
    /// and require SharedObjects need the kOS version, some (for example GlobalPath) need only the lighter kOS.Safe version. SafeSerializationMgr
    /// and SerializationMgr will both will both now supply an instance of kOS.Safe.SharedObjects to classes that implement this interface.
    /// </summary>
    public interface IHasSafeSharedObjects
    {
        SafeSharedObjects Shared { set; }
    }
}

