using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    public class MockStructure : Encapsulation.Structure
    {
         
    }

    [TestFixture]
    public class NoArgsSuffixTest
    {
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

            var value = del.Value;
            Assert.IsNotNull(value);
            Assert.AreSame(obj,value);
        }

        [Test]
        public void CanGetDelegateValueType()
        {
            const int VALUE = 12345;
            var suffix = new NoArgsSuffix<Encapsulation.Structure>(() => new ScalarIntValue(VALUE) );
            var del = suffix.Get();
            Assert.IsNotNull(del);

            var value = del.Value;
            Assert.IsNotNull(value);
            Assert.IsInstanceOf<int>(value);
            Assert.AreEqual(VALUE,value);
        }

    }
}
