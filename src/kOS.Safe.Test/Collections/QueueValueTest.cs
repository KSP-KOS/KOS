using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class QueueValueTest
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
            Assert.AreEqual(0,length);

            InvokeDelegate(queue, "PUSH", "value1");
            InvokeDelegate(queue, "PUSH", "value2");

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(2,length);

            object popped = InvokeDelegate(queue, "POP");
            Assert.AreEqual("value1", popped);

            popped = InvokeDelegate(queue, "POP");
            Assert.AreEqual("value2", popped);

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(0, length);
        }

        [Test]
        public void CanClear()
        {
            var queue = new QueueValue();

            InvokeDelegate(queue, "PUSH", 1);
            InvokeDelegate(queue, "PUSH", new object());

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(2,length);
            InvokeDelegate(queue, "CLEAR");
            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(0,length);
        }

        [Test]
        public void CopyIsACopy()
        {
            var queue = new QueueValue();

            var zedObject = new object();
            InvokeDelegate(queue, "PUSH", zedObject);
            var firstObject = new object();
            InvokeDelegate(queue, "PUSH", firstObject);
            var secondObject = new object();
            InvokeDelegate(queue, "PUSH", secondObject);
            var thirdObject = new object();
            InvokeDelegate(queue, "PUSH", thirdObject);

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(4,length);

            var copy = InvokeDelegate(queue, "COPY") as QueueValue;
            Assert.AreNotSame(queue, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(4,copyLength);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(0,copyLength);

            length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(4,length);
        }

        [Test]
        public void CanTestContains()
        {
            var queue = new QueueValue();

            var zedObject = new object();
            InvokeDelegate(queue, "PUSH", zedObject);
            var firstObject = new object();
            InvokeDelegate(queue, "PUSH", firstObject);
            var secondObject = new object();
            var thirdObject = new object();

            var length = InvokeDelegate(queue, "LENGTH");
            Assert.AreEqual(2,length);


            Assert.IsTrue((bool)InvokeDelegate(queue, "CONTAINS", zedObject));
            Assert.IsTrue((bool)InvokeDelegate(queue, "CONTAINS", firstObject));
            Assert.IsFalse((bool)InvokeDelegate(queue, "CONTAINS", secondObject));
            Assert.IsFalse((bool)InvokeDelegate(queue, "CONTAINS", thirdObject));
        }

        private object InvokeDelegate(IDumper queue, string suffixName, params object[] parameters)
        {
            var lengthObj = queue.GetSuffix(suffixName);
            Assert.IsNotNull(lengthObj);
            var lengthDelegate = lengthObj as Delegate;
            Assert.IsNotNull(lengthDelegate);
            var length = lengthDelegate.DynamicInvoke(parameters);
            return length;
        }
    }
}
