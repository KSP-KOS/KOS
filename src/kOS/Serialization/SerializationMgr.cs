using System;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using kOS.Safe.Exceptions;
using kOS.Safe;

namespace kOS.Serialization
{
    public class SerializationMgr : SafeSerializationMgr
    {
        private readonly SharedObjects sharedObjects;

        public SerializationMgr(SharedObjects sharedObjects)
        {
            this.sharedObjects = sharedObjects;
        }

        public override SerializableStructure CreateInstance(string typeFullName, Dump data)
        {
            var deserializedType = Type.GetType(typeFullName) ??
                                   Type.GetType(typeFullName + ", " + typeof(SafeSerializationMgr).Assembly.FullName);

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type: " + typeFullName);
            }

            SerializableStructure instance = Activator.CreateInstance(deserializedType) as SerializableStructure;

            if (instance is IHasSharedObjects)
            {
                IHasSharedObjects withSharedObjects = instance as IHasSharedObjects;
                withSharedObjects.Shared = sharedObjects;
            }

            if (instance != null)
            {
                instance.LoadDump(data);
            }

            return instance;
        }
    }
}

