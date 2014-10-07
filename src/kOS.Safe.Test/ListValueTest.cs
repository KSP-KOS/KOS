using System;
using NUnit.Framework;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class ListValueTest
    {
        [Test]
        public void CanCreate()
        {
            var list = new ListValue();
            Assert.IsNotNull(list);
        }

        [Test]
        public void CanAddItem()
        {
            var list = new ListValue();
            Assert.IsNotNull(list);
            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(0,length);

            InvokeDelegate(list, "ADD", new object());

            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(1,length);
        }

        [Test]
        public void CanClear()
        {
            var list = new ListValue();

            InvokeDelegate(list, "ADD", new object());
            InvokeDelegate(list, "ADD", new object());

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(2,length);
            InvokeDelegate(list, "CLEAR");
            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(0,length);
        }

        [Test]
        public void CanGetIndex()
        {
            var list = new ListValue();

            var zedObject = new object();
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = new object();
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = new object();
            InvokeDelegate(list, "ADD", secondObject);
            var thirdObject = new object();
            InvokeDelegate(list, "ADD", thirdObject);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(4,length);

            IIndexable indexable = list;
            Assert.AreSame(zedObject, indexable.GetIndex(0));
            Assert.AreSame(firstObject, indexable.GetIndex(1));
            Assert.AreSame(secondObject, indexable.GetIndex(2));
            Assert.AreSame(thirdObject, indexable.GetIndex(3));
            Assert.AreNotSame(indexable.GetIndex(0),indexable.GetIndex(1));
            Assert.AreNotSame(indexable.GetIndex(0),indexable.GetIndex(2));
            Assert.AreNotSame(indexable.GetIndex(0),indexable.GetIndex(3));
            Assert.AreNotSame(indexable.GetIndex(1),indexable.GetIndex(2));
            Assert.AreNotSame(indexable.GetIndex(1),indexable.GetIndex(3));
            Assert.AreNotSame(indexable.GetIndex(2),indexable.GetIndex(3));
        }

        [Test]
        public void CopyIsACopy()
        {
            var list = new ListValue();

            var zedObject = new object();
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = new object();
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = new object();
            InvokeDelegate(list, "ADD", secondObject);
            var thirdObject = new object();
            InvokeDelegate(list, "ADD", thirdObject);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(4,length);

            var copy = InvokeDelegate(list, "COPY") as ListValue;
            Assert.AreNotSame(list, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(4,copyLength);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(0,copyLength);

            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(4,length);
        }

        [Test]
        public void CanTestContains()
        {
            var list = new ListValue();

            var zedObject = new object();
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = new object();
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = new object();
            var thirdObject = new object();

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(2,length);


            Assert.IsTrue((bool)InvokeDelegate(list, "CONTAINS", zedObject));
            Assert.IsTrue((bool)InvokeDelegate(list, "CONTAINS", firstObject));
            Assert.IsFalse((bool)InvokeDelegate(list, "CONTAINS", secondObject));
            Assert.IsFalse((bool)InvokeDelegate(list, "CONTAINS", thirdObject));
        }


        private object InvokeDelegate(ListValue list, string suffixName, params object[] parameters)
        {
            var lengthObj = list.GetSuffix(suffixName);
            Assert.IsNotNull(lengthObj);
            var lengthDelegate = lengthObj as Delegate;
            Assert.IsNotNull(lengthDelegate);
            var length = lengthDelegate.DynamicInvoke(parameters);
            return length;
        }
    }
}
