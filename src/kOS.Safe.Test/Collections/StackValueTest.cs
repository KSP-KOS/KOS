using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class StackValueTest
    {
        [Test]
        public void CanCreate()
        {
            var stack = new StackValue();
            Assert.IsNotNull(stack);
        }

        [Test]
        public void CanPushPopItem()
        {
            var stack = new StackValue();
            Assert.IsNotNull(stack);
            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(0,length);

            InvokeDelegate(stack, "PUSH", "value1");
            InvokeDelegate(stack, "PUSH", "value2");

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(2,length);

            object popped = InvokeDelegate(stack, "POP");
            Assert.AreEqual("value2", popped);

            popped = InvokeDelegate(stack, "POP");
            Assert.AreEqual("value1", popped);

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(0, length);
        }

        [Test]
        public void CanClear()
        {
            var stack = new StackValue();

            InvokeDelegate(stack, "PUSH", 1);
            InvokeDelegate(stack, "PUSH", new object());

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(2,length);
            InvokeDelegate(stack, "CLEAR");
            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(0,length);
        }

        [Test]
        public void CopyIsACopy()
        {
            var stack = new StackValue();

            var zedObject = new object();
            InvokeDelegate(stack, "PUSH", zedObject);
            var firstObject = new object();
            InvokeDelegate(stack, "PUSH", firstObject);
            var secondObject = new object();
            InvokeDelegate(stack, "PUSH", secondObject);
            var thirdObject = "third";
            InvokeDelegate(stack, "PUSH", thirdObject);

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(4,length);

            var copy = InvokeDelegate(stack, "COPY") as StackValue;
            Assert.AreNotSame(stack, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(4,copyLength);

            object popped = InvokeDelegate(copy, "POP");
            Assert.AreEqual(thirdObject, popped);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(0,copyLength);

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(4,length);
        }

        [Test]
        public void CanTestContains()
        {
            var stack = new StackValue();

            var zedObject = new object();
            InvokeDelegate(stack, "PUSH", zedObject);
            var firstObject = new object();
            InvokeDelegate(stack, "PUSH", firstObject);
            var secondObject = new object();
            var thirdObject = new object();

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(2,length);


            Assert.IsTrue((bool)InvokeDelegate(stack, "CONTAINS", zedObject));
            Assert.IsTrue((bool)InvokeDelegate(stack, "CONTAINS", firstObject));
            Assert.IsFalse((bool)InvokeDelegate(stack, "CONTAINS", secondObject));
            Assert.IsFalse((bool)InvokeDelegate(stack, "CONTAINS", thirdObject));
        }

        private object InvokeDelegate(IDumper stack, string suffixName, params object[] parameters)
        {
            var lengthObj = stack.GetSuffix(suffixName);
            Assert.IsNotNull(lengthObj);
            var lengthDelegate = lengthObj as Delegate;
            Assert.IsNotNull(lengthDelegate);
            var length = lengthDelegate.DynamicInvoke(parameters);
            return length;
        }
    }
}
