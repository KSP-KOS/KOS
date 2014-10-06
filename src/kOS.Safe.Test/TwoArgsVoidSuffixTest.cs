using System;
using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;
using NSubstitute;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class TwoArgsVoidSuffixTest
    {
        [Test]
        public void CanCreate()
        {
            var suffix = new TwoArgsVoidSuffix<object, object>((one, two) => { });
            Assert.IsNotNull(suffix.Get());
        }

        [Test]
        public void CanGetDelegate()
        {
            var suffix = new TwoArgsVoidSuffix<object, object>((one, two) => { });
            var del = suffix.Get();
            Assert.IsNotNull(del);
            var delegateAsDelegate = del as Delegate;
            Assert.IsNotNull(delegateAsDelegate);
        }

        [Test]
        public void CanExecuteDelegate()
        {
            var mockDel = Substitute.For<TwoArgsVoidSuffix<object, object>.Del<object, object>>();

            var suffix = new TwoArgsVoidSuffix<object, object>(mockDel);
            var del = suffix.Get();
            Assert.IsNotNull(del);
            var delegateAsDelegate = del as Delegate;
            Assert.IsNotNull(delegateAsDelegate);
            delegateAsDelegate.DynamicInvoke(new object(), new object());

            mockDel.ReceivedWithAnyArgs(1);
        }

    }
}
