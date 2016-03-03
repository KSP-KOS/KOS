using System;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Safe;

namespace kOS.Serialization
{
    public class SerializationMgr : SafeSerializationMgr
    {
        private readonly SharedObjects sharedObjects;

        static SerializationMgr() {
            SafeSerializationMgr.AddAssembly(typeof(SerializationMgr).Assembly.FullName);
        }


        public SerializationMgr(SharedObjects sharedObjects)
        {
            this.sharedObjects = sharedObjects;
        }


        public override SerializableStructure CreateAndLoad(string typeFullName, Dump data)
        {
            SerializableStructure instance = base.CreateInstance(typeFullName);

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

