using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class StackValueTest : CollectionValueTest
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
            Assert.AreEqual(new ScalarIntValue(0), length);

            InvokeDelegate(stack, "PUSH", new StringValue("value1"));
            InvokeDelegate(stack, "PUSH", new StringValue("value2"));

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);

            object popped = InvokeDelegate(stack, "POP");
            Assert.AreEqual(new StringValue("value2"), popped);

            popped = InvokeDelegate(stack, "POP");
            Assert.AreEqual(new StringValue("value1"), popped);

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);
        }

        [Test]
        public void CanClear()
        {
            var stack = new StackValue();

            InvokeDelegate(stack, "PUSH", new ScalarIntValue(1));
            InvokeDelegate(stack, "PUSH", new ScalarIntValue(2));

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(2),length);
            InvokeDelegate(stack, "CLEAR");
            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(0),length);
        }

        [Test]
        public void CopyIsACopy()
        {
            var stack = new StackValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(stack, "PUSH", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(stack, "PUSH", firstObject);
            var secondObject = ScalarIntValue.Two;
            InvokeDelegate(stack, "PUSH", secondObject);
            var thirdObject = new StringValue("third");
            InvokeDelegate(stack, "PUSH", thirdObject);

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);

            var copy = InvokeDelegate(stack, "COPY") as StackValue;
            Assert.AreNotSame(stack, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4),copyLength);

            object popped = InvokeDelegate(copy, "POP");
            Assert.AreEqual(thirdObject, popped);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, copyLength);

            length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);
        }

        [Test]
        public void CanTestContains()
        {
            var stack = new StackValue();

            var zedObject = new StringValue("abc");
            InvokeDelegate(stack, "PUSH", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(stack, "PUSH", firstObject);
            var secondObject = ScalarIntValue.Two;
            var thirdObject = new ScalarIntValue(4);

            var length = InvokeDelegate(stack, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);

            Assert.IsTrue((BooleanValue)InvokeDelegate(stack, "CONTAINS", zedObject));
            Assert.IsTrue((BooleanValue)InvokeDelegate(stack, "CONTAINS", firstObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(stack, "CONTAINS", secondObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(stack, "CONTAINS", thirdObject));
        }
    }
}
