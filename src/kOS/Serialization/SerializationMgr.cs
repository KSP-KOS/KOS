using System;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using kOS.Safe.Exceptions;

namespace kOS.Serialization
{
    public class SerializationMgr : SafeSerializationMgr
    {
        private readonly SharedObjects sharedObjects;

        public SerializationMgr(SharedObjects sharedObjects)
        {
            this.sharedObjects = sharedObjects;
        }

        public override IDumper CreateInstance(string typeFullName, IDictionary<object, object> data)
        {
            var deserializedType = Type.GetType(typeFullName) ??
                                   Type.GetType(typeFullName + ", " + typeof(SafeSerializationMgr).Assembly.FullName);

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type2: " + typeFullName);
            }

            IDumper instance = Activator.CreateInstance(deserializedType) as IDumper;

            if (instance is IDumperWithSharedObjects)
            {
                IDumperWithSharedObjects withSharedObjects = instance as IDumperWithSharedObjects;
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

