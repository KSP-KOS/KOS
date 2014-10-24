using System;
using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class NoArgsSuffixTest
    {
        [Test]
        public void CanCreate()
        {
            var suffix = new NoArgsSuffix<object>(() => new object() );
            Assert.IsNotNull(suffix.Get());
        }

        [Test]
        public void CanGetDelegate()
        {
            var obj = new object();
            var suffix = new NoArgsSuffix<object>(() => obj );
            var del = suffix.Get();
            Assert.IsNotNull(del);
            var delegateAsDelegate = del as Delegate;
            Assert.IsNotNull(delegateAsDelegate);
        }

        [Test]
        public void CanGetDelegateValue()
        {
            var obj = new object();
            var suffix = new NoArgsSuffix<object>(() => obj );
            var del = suffix.Get();
            Assert.IsNotNull(del);
            var delegateAsDelegate = del as Delegate;
            Assert.IsNotNull(delegateAsDelegate);
            var value = delegateAsDelegate.DynamicInvoke();
            Assert.IsNotNull(value);
            Assert.AreSame(obj,value);
        }

        [Test]
        public void CanGetDelegateValueType()
        {
            const int VALUE = 12345;
            var suffix = new NoArgsSuffix<object>(() => VALUE );
            var del = suffix.Get();
            Assert.IsNotNull(del);
            var delegateAsDelegate = del as Delegate;
            Assert.IsNotNull(delegateAsDelegate);
            var value = delegateAsDelegate.DynamicInvoke();
            Assert.IsNotNull(value);
            Assert.IsInstanceOf<int>(value);
            Assert.AreEqual(VALUE,value);
        }

    }
}
