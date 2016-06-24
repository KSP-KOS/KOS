using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class UniqueSetValueTest : CollectionValueTest
    {
        [Test]
        public void CanCreate()
        {
            var set = new UniqueSetValue();
            Assert.IsNotNull(set);
        }

        [Test]
        public void CanAddAndRemoveItems()
        {
            var set = new UniqueSetValue();
            Assert.IsNotNull(set);
            var length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);

            InvokeDelegate(set, "ADD", new StringValue("value1"));
            InvokeDelegate(set, "ADD", new StringValue("value1"));
            InvokeDelegate(set, "ADD", new StringValue("value2"));

            length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);

            object result = InvokeDelegate(set, "REMOVE", new StringValue("value1"));
            Assert.AreEqual(BooleanValue.True, result);

            length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.One, length);

            result = InvokeDelegate(set, "REMOVE", new StringValue("value1"));
            Assert.AreEqual(BooleanValue.False, result);

            result = InvokeDelegate(set, "REMOVE", new StringValue("value2"));
            Assert.AreEqual(BooleanValue.True, result);

            length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, length);
        }

        [Test]
        public void CanClear()
        {
            var set = new UniqueSetValue();

            InvokeDelegate(set, "ADD", new ScalarIntValue(1));
            InvokeDelegate(set, "ADD", new ScalarIntValue(2));

            var length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two,length);
            InvokeDelegate(set, "CLEAR");
            length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero,length);
        }

        [Test]
        public void CopyIsACopy()
        {
            var set = new UniqueSetValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(set, "ADD", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(set, "ADD", firstObject);
            var secondObject = ScalarIntValue.Two;
            InvokeDelegate(set, "ADD", secondObject);
            var thirdObject = new StringValue("third");
            InvokeDelegate(set, "ADD", thirdObject);

            var length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);

            var copy = InvokeDelegate(set, "COPY") as UniqueSetValue;
            Assert.AreNotSame(set, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4),copyLength);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, copyLength);

            length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);
        }

        [Test]
        public void CanTestContains()
        {
            var set = new UniqueSetValue();

            var zedObject = new StringValue("abc");
            InvokeDelegate(set, "ADD", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(set, "ADD", firstObject);
            var secondObject = ScalarIntValue.Two;
            var thirdObject = new ScalarIntValue(4);

            var length = InvokeDelegate(set, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);

            Assert.IsTrue((BooleanValue)InvokeDelegate(set, "CONTAINS", zedObject));
            Assert.IsTrue((BooleanValue)InvokeDelegate(set, "CONTAINS", firstObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(set, "CONTAINS", secondObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(set, "CONTAINS", thirdObject));
        }
    }
}
