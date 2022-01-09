using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe.Test.Opcode;

namespace kOS.Safe.Test
{
    public abstract class CollectionValueTest
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
        }

        protected object InvokeDelegate(kOS.Safe.Encapsulation.Structure stack, string suffixName, params object[] parameters)
        {
            var lengthObj = stack.GetSuffix(suffixName) as DelegateSuffixResult;
            Assert.IsNotNull(lengthObj);

            cpu.PushArgumentStack(null); // fake delegate info
            cpu.PushArgumentStack(new KOSArgMarkerType());
            foreach (object param in parameters)
            {
                cpu.PushArgumentStack(param);
            }

            lengthObj.Invoke(cpu);

            return lengthObj.Value;
        }
    }
}

