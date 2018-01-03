using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using kOS.Safe.Test.Opcode;
using NSubstitute;
using NUnit.Framework;
using System;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class TwoArgsVoidSuffixTest
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
        }

        [Test]
        public void CanCreate()
        {
            var suffix = new TwoArgsSuffix<kOS.Safe.Encapsulation.Structure, kOS.Safe.Encapsulation.Structure>((one, two) => { });
            Assert.IsNotNull(suffix.Get());
        }

        private delegate void TwoArgs(kOS.Safe.Encapsulation.Structure a1,kOS.Safe.Encapsulation.Structure a2);

        [Test]
        public void CanExecuteDelegate()
        {
            var mockDel = Substitute.For<TwoArgsSuffix<kOS.Safe.Encapsulation.Structure, kOS.Safe.Encapsulation.Structure>.Del<kOS.Safe.Encapsulation.Structure, kOS.Safe.Encapsulation.Structure>>();

            // Wrap it in a lambda to avoid the weird third closure arg
            var suffix = new TwoArgsSuffix<kOS.Safe.Encapsulation.Structure, kOS.Safe.Encapsulation.Structure>((one, two) => mockDel(one, two));
            var del = suffix.Get() as DelegateSuffixResult;
            Assert.IsNotNull(del);

            // Add fake arguments to the stack for the call
            cpu.PushArgumentStack(null);
            cpu.PushArgumentStack(new KOSArgMarkerType());
            cpu.PushArgumentStack(ScalarIntValue.Zero);
            cpu.PushArgumentStack(ScalarIntValue.Zero);

            del.Invoke(cpu);

            mockDel.ReceivedWithAnyArgs(1);
        }
    }
}