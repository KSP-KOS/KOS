using System;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using NUnit.Framework;

namespace kOS.Safe.Test.Opcode
{
    [TestFixture]
    public class OpcodeSetIndexTest
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
        }

        [Test]
        public void CanSetListIndex()
        {
            var list = new ListValue();
            cpu.PushStack(list);

            const string VALUE = "foo";
            cpu.PushStack(VALUE);
            
            const int INDEX = 0;
            cpu.PushStack(INDEX);

            var opcode = new OpcodeSetIndex();

            opcode.Execute(cpu);

            Assert.AreEqual(1, list.Count);
        }
    }
}
