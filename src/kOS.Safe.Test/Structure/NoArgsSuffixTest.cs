using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;
using kOS.Safe.Test.Opcode;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test.Structure
{
    [KOSNomenclature("MockStructure")]
    public class MockStructure : Encapsulation.Structure
    {
         
    }

    [TestFixture]
    public class NoArgsSuffixTest
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
            var suffix = new NoArgsSuffix<Encapsulation.Structure>(() => new MockStructure() );
            Assert.IsNotNull(suffix.Get());
        }

        [Test]
        public void CanGetDelegate()
        {
            var obj = new MockStructure();
            var suffix = new NoArgsSuffix<Encapsulation.Structure>(() => obj );
            var del = suffix.Get();
            Assert.IsNotNull(del);
        }

        [Test]
        public void CanGetDelegateValue()
        {
            var obj = new MockStructure();
            var suffix = new NoArgsSuffix<Encapsulation.Structure>(() => obj );
            var del = suffix.Get();
            Assert.IsNotNull(del);

            cpu.PushArgumentStack(null);  // dummy variable for ReverseStackArgs to pop
            cpu.PushArgumentStack(new KOSArgMarkerType());
            del.Invoke(cpu);

            var value = del.Value;
            Assert.IsNotNull(value);
            Assert.AreSame(obj,value);
        }

        [Test]
        public void CanGetDelegateValueType()
        {
            const int VALUE = 12345;
            var suffix = new NoArgsSuffix<Encapsulation.Structure>(() => ScalarValue.Create(VALUE));
            var del = suffix.Get();
            Assert.IsNotNull(del);

            cpu.PushArgumentStack(null);  // dummy variable for ReverseStackArgs to pop
            cpu.PushArgumentStack(new KOSArgMarkerType());
            del.Invoke(cpu);

            var value = del.Value;
            Assert.IsNotNull(value);
            Assert.IsInstanceOf<ScalarValue>(value);
            Assert.AreEqual(ScalarValue.Create(VALUE), value);
        }

    }
}
