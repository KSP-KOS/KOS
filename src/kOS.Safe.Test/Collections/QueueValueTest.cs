using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class QueueValueTest : CollectionValueTest
    {
        [Test]
        public void CanCreate()
        {
            var queue = new QueueValue();
            Assert.IsNotNull(queue);
        }

        [Test]
        public void CanPushPopItem()
        {
            var queue = new QueueValue();
            Assert.IsNotNull(queue);
            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);

            InvokeDelegate(queue, "PUSH", new StringValue("value1"));
            InvokeDelegate(queue, "PUSH", new StringValue("value2"));

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);

            object popped = InvokeDelegate(queue, "POP");
            Assert.AreEqual(new StringValue("value1"), popped);

            popped = InvokeDelegate(queue, "POP");
            Assert.AreEqual(new StringValue("value2"), popped);

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);
        }

        [Test]
        public void CanClear()
        {
            var queue = new QueueValue();

            InvokeDelegate(queue, "PUSH", ScalarIntValue.One);
            InvokeDelegate(queue, "PUSH", ScalarIntValue.Two);

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);
            InvokeDelegate(queue, "CLEAR");
            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);
        }

        [Test]
        public void CopyIsACopy()
        {
            var queue = new QueueValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(queue, "PUSH", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(queue, "PUSH", firstObject);
            var secondObject = ScalarIntValue.Two;
            InvokeDelegate(queue, "PUSH", secondObject);
            var thirdObject = new ScalarIntValue(4);
            InvokeDelegate(queue, "PUSH", thirdObject);

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);

            var copy = InvokeDelegate(queue, "COPY") as QueueValue;
            Assert.AreNotSame(queue, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), copyLength);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, copyLength);

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);
        }

        [Test]
        public void CanTestContains()
        {
            var queue = new QueueValue();

            var zedObject = new StringValue("abc");
            InvokeDelegate(queue, "PUSH", zedObject);
            var firstObject = new StringValue("def");
            InvokeDelegate(queue, "PUSH", firstObject);
            var secondObject = new StringValue("xyz");
            var thirdObject = ScalarIntValue.Zero;

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);


            Assert.IsTrue((BooleanValue)InvokeDelegate(queue, "CONTAINS", zedObject));
            Assert.IsTrue((BooleanValue)InvokeDelegate(queue, "CONTAINS", firstObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(queue, "CONTAINS", secondObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(queue, "CONTAINS", thirdObject));
        }
    }
}
