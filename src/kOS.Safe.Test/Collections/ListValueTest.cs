using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using kOS.Safe.Test.Opcode;
using NUnit.Framework;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class ListValueTest : CollectionValueTest
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
            Assert.AreEqual(ScalarIntValue.Zero, length);

            InvokeDelegate(list, "ADD", ScalarIntValue.Zero);

            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(ScalarIntValue.One, length);
        }

        [Test]
        public void CanClear()
        {
            var list = new ListValue();

            InvokeDelegate(list, "ADD", ScalarIntValue.Zero);
            InvokeDelegate(list, "ADD", ScalarIntValue.Zero);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two,length);
            InvokeDelegate(list, "CLEAR");
            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero,length);
        }

        [Test]
        public void CanGetIndex()
        {
            var list = new ListValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = ScalarIntValue.Two;
            InvokeDelegate(list, "ADD", secondObject);
            var thirdObject = new ScalarIntValue(4);
            InvokeDelegate(list, "ADD", thirdObject);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4),length);

            Assert.AreSame(zedObject, list[0]);
            Assert.AreSame(firstObject, list[1]);
            Assert.AreSame(secondObject, list[2]);
            Assert.AreSame(thirdObject, list[3]);
            Assert.AreNotSame(list[0], list[1]);
            Assert.AreNotSame(list[0], list[2]);
            Assert.AreNotSame(list[0], list[3]);
            Assert.AreNotSame(list[1], list[2]);
            Assert.AreNotSame(list[1], list[3]);
            Assert.AreNotSame(list[2], list[3]);
        }

        [Test]
        public void CopyIsACopy()
        {
            var list = new ListValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = ScalarIntValue.Two;
            InvokeDelegate(list, "ADD", secondObject);
            var thirdObject = new ScalarIntValue(4);
            InvokeDelegate(list, "ADD", thirdObject);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);

            var copy = InvokeDelegate(list, "COPY") as ListValue;
            Assert.AreNotSame(list, copy);

            var copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), copyLength);

            InvokeDelegate(copy, "CLEAR");

            copyLength = InvokeDelegate(copy, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Zero, copyLength);

            length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(new ScalarIntValue(4), length);
        }

        [Test]
        public void CanTestContains()
        {
            var list = new ListValue();

            var zedObject = ScalarIntValue.Zero;
            InvokeDelegate(list, "ADD", zedObject);
            var firstObject = ScalarIntValue.One;
            InvokeDelegate(list, "ADD", firstObject);
            var secondObject = ScalarIntValue.Two;
            var thirdObject = new ScalarIntValue(4);

            var length = InvokeDelegate(list, "LENGTH");
            Assert.AreEqual(ScalarIntValue.Two, length);


            Assert.IsTrue((BooleanValue)InvokeDelegate(list, "CONTAINS", zedObject));
            Assert.IsTrue((BooleanValue)InvokeDelegate(list, "CONTAINS", firstObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(list, "CONTAINS", secondObject));
            Assert.IsFalse((BooleanValue)InvokeDelegate(list, "CONTAINS", thirdObject));
        }
        
        /// <summary>
        /// Creates a complex example of a nested list of lists and other
        /// things, to use in some of the tests to prove complex example cases.
        /// Returns a list that looks like so:
        /// <pre>
        /// list {
        ///     100,
        ///     200,
        ///     list {
        ///         list {
        ///             "inner string 1",
        ///             2
        ///         },
        ///         "string,one.two",
        ///         "string,one.three"
        ///     },
        ///     list {
        ///         "string,two.one",
        ///         "string,two.two"
        ///     },
        ///     "String, outer value"
        /// }
        /// </pre>
        /// This should be sufficiently complex to work with for testing a variety of cases.
        /// 
        /// </summary>
        /// <returns>A list containing the description above</returns>
        private ListValue MakeNestedExample()
        {
            const string OUTER_STRING = "String, outer value";
            
            ListValue list = new ListValue();
            ListValue innerList1 = new ListValue();
            ListValue innerList2 = new ListValue();
            ListValue innerInnerList = new ListValue
            {
                new StringValue("inner string 1"),
                new ScalarIntValue(2)
            };


            innerList1.Add( innerInnerList );
            innerList1.Add( new StringValue("string,one.two") );
            innerList1.Add( new StringValue("string,one.three") );

            innerList2.Add( new StringValue("string,two.one") );
            innerList2.Add( new StringValue("string,two.two") );
            
            InvokeDelegate(list,"ADD", new ScalarIntValue(100));
            InvokeDelegate(list,"ADD", new ScalarIntValue(200));
            InvokeDelegate(list,"ADD", innerList1);            
            InvokeDelegate(list,"ADD", innerList2);            
            InvokeDelegate(list,"ADD", OUTER_STRING);
            
            return list;
        }
        
        [Test]
        public void EachListConstructor()
        {
            var cpu = new FakeCpu();
            cpu.PushArgumentStack(new KOSArgMarkerType());

            var baseList = new ListValue();
            var baseDelegate = baseList.GetSuffix("LENGTH");
            cpu.PushArgumentStack(null); // dummy push to be popped by ReverseStackArgs
            cpu.PushArgumentStack(new KOSArgMarkerType());
            baseDelegate.Invoke(cpu);
            Assert.AreEqual(ScalarIntValue.Zero, baseDelegate.Value);

            var castList = ListValue.CreateList(new List<object>());
            var castDelegate = castList.GetSuffix("LENGTH");
            cpu.PushArgumentStack(null); // dummy push to be popped by ReverseStackArgs
            cpu.PushArgumentStack(new KOSArgMarkerType());
            castDelegate.Invoke(cpu);
            Assert.AreEqual(ScalarIntValue.Zero, castDelegate.Value);

            var copyDelegate = baseList.GetSuffix("COPY");
            cpu.PushArgumentStack(null); // dummy push to be popped by ReverseStackArgs
            cpu.PushArgumentStack(new KOSArgMarkerType());
            copyDelegate.Invoke(cpu);
            var copyList = copyDelegate.Value;
            Assert.AreEqual(baseList, copyList);

            var lengthDelegate = copyList.GetSuffix("LENGTH");
            cpu.PushArgumentStack(null); // dummy push to be popped by ReverseStackArgs
            cpu.PushArgumentStack(new KOSArgMarkerType());
            lengthDelegate.Invoke(cpu);
            Assert.AreEqual(ScalarIntValue.Zero, lengthDelegate.Value);
        }
    }
}
