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

        public SerializationMgr(SharedObjects sharedObjects) : base(sharedObjects)
        {
            SafeSerializationMgr.AddAssembly(typeof(SerializationMgr).Assembly.FullName);
        }

        public override IDumper CreateAndLoad(string typeFullName, Dump data)
        {
            IDumper instance = base.CreateInstance(typeFullName);

            if (instance is IHasSharedObjects)
            {
                IHasSharedObjects withSharedObjects = instance as IHasSharedObjects;
                withSharedObjects.Shared = sharedObjects;
            }
            else if (instance is IHasSafeSharedObjects)
            {
                IHasSafeSharedObjects withSharedObjects = instance as IHasSafeSharedObjects;
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

