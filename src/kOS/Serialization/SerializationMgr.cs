using System;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace kOS.Serialization
{
    public class SerializationMgr : SafeSerializationMgr
    {
        private SharedObjects sharedObjects;

        public SerializationMgr(SharedObjects sharedObjects) : base()
        {
            this.sharedObjects = sharedObjects;
        }

        public override IDumper CreateInstance(string typeFullName, IDictionary<object, object> data)
        {
            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                SafeHouse.Logger.Log("#### " + typeFullName + ", " + typeof(SafeSerializationMgr).Assembly.FullName);
                deserializedType = Type.GetType(typeFullName + ", " + typeof(SafeSerializationMgr).Assembly.FullName);
            }

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type2: " + typeFullName);
            }

            IDumper instance = Activator.CreateInstance(deserializedType) as IDumper;

            if (instance is IDumperWithSharedObjects)
            {
                IDumperWithSharedObjects withSharedObjects = instance as IDumperWithSharedObjects;
                withSharedObjects.SetSharedObjects(sharedObjects);
            }

            instance.LoadDump(data);

            return instance;
        }
    }
}

