using System;
using System.Reflection;
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
            this.sharedObjects = (SharedObjects)sharedObjects;
        }

        public override IDumper CreateAndLoad(string typeFullName, Dump data)
        {
            Type loadedType = base.GetTypeFromFullname(typeFullName);
            MethodInfo method = loadedType.GetMethod("CreateFromDump", new Type[] { typeof(SafeSharedObjects), typeof(Dump) });
            IDumper instance = (IDumper)method.Invoke(null, new object[] { sharedObjects, data });
            return instance;
        }
    }
}

