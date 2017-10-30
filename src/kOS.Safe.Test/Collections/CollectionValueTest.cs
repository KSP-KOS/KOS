using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test
{
    public abstract class CollectionValueTest
    {

        protected object InvokeDelegate(SerializableStructure stack, string suffixName, params object[] parameters)
        {
            var lengthObj = stack.GetSuffix(suffixName) as DelegateSuffixResult;
            Assert.IsNotNull(lengthObj);
            Assert.IsNotNull(lengthObj.Del);
            return lengthObj.Del.DynamicInvoke(parameters);
        }
    }
}

