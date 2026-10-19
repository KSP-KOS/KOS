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

        private BooleanValue InvokeWithBooleanParam(object arg)
        {
            BooleanValue received = null;
            var suffix = new TwoArgsSuffix<StringValue, BooleanValue>((one, two) => { received = two; });
            var del = suffix.Get() as DelegateSuffixResult;
            Assert.IsNotNull(del);

            cpu.PushArgumentStack(null);
            cpu.PushArgumentStack(new KOSArgMarkerType());
            cpu.PushArgumentStack(new StringValue("toggle"));
            cpu.PushArgumentStack(arg);

            del.Invoke(cpu);
            return received;
        }

        [Test]
        public void CanConvertScalarToBooleanArg()
        {
            Assert.IsTrue(InvokeWithBooleanParam(ScalarIntValue.One));
            Assert.IsFalse(InvokeWithBooleanParam(ScalarIntValue.Zero));
            Assert.IsTrue(InvokeWithBooleanParam(ScalarValue.Create(0.5)));
            Assert.IsTrue(InvokeWithBooleanParam(ScalarValue.Create(-1)));
        }

        [Test]
        public void CanConvertStringToBooleanArg()
        {
            Assert.IsFalse(InvokeWithBooleanParam(new StringValue("")));
            Assert.IsTrue(InvokeWithBooleanParam(new StringValue("false")));
        }

        [Test]
        public void ThrowsKOSCastExceptionForUnconvertibleBooleanArg()
        {
            Assert.Throws<kOS.Safe.Exceptions.KOSCastException>(() => InvokeWithBooleanParam(new Lexicon()));
        }

        [Test]
        public void ThrowsKOSCastExceptionForUnconvertibleArg()
        {
            var suffix = new TwoArgsSuffix<ScalarValue, ScalarValue>((one, two) => { });
            var del = suffix.Get() as DelegateSuffixResult;

            cpu.PushArgumentStack(null);
            cpu.PushArgumentStack(new KOSArgMarkerType());
            cpu.PushArgumentStack(ScalarIntValue.Zero);
            cpu.PushArgumentStack(new StringValue("abc"));

            Assert.Throws<kOS.Safe.Exceptions.KOSCastException>(() => del.Invoke(cpu));
        }
    }
}