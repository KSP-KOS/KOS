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
        private ListValue makeNestedExample()
        {
            ListValue list = new ListValue();
            
            string str1 = "String, outer value";
            ListValue innerList1 = new ListValue();
            ListValue innerList2 = new ListValue();
            ListValue innerInnerList = new ListValue();
            
            innerInnerList.Add( "inner string 1");
            innerInnerList.Add( 2 );
            
            innerList1.Add( innerInnerList );
            innerList1.Add( "string,one.two" );
            innerList1.Add( "string,one.three" );

            innerList2.Add( "string,two.one" );
            innerList2.Add( "string,two.two" );
            
            InvokeDelegate(list,"ADD", 100);
            InvokeDelegate(list,"ADD", 200);
            InvokeDelegate(list,"ADD", innerList1);            
            InvokeDelegate(list,"ADD", innerList2);            
            InvokeDelegate(list,"ADD", str1);
            
            return list;
        }
        
        [Test]
        public void CanShallowToString()
        {
            ListValue list = makeNestedExample();
            
            string result = list.ToString();
            
            Assert.IsTrue(result.Contains("100"),"CanShallowToString(): ToString from list isn't shallow enough and is finding number 100\n"+result);
            Assert.IsTrue(result.Contains("String, outer value"),"CanShallowToString(): ToString from list isn't shallow enough and is finding \"string,outer value\"\n"+result);
            Assert.IsTrue(result.Contains("LIST of 5 items"),"CanShallowToString(): failed to find expected inner list object terse dump\n"+result);
            Assert.IsFalse(result.Contains("string,one.two"),"CanShallowToString(): ToString from list isn't shallow enough and is finding inner component 'string,one.two'\n"+result);
        }

        [Test]
        public void CanDeepToString()
        {
            ListValue list = makeNestedExample();
            
            string result = (string)InvokeDelegate(list, "DUMP");
            
            Assert.IsTrue(result.Contains("100"),"CanDeepToString(): failed to find expected integer 100 in string output\n"+result);
            Assert.IsTrue(result.Contains("String, outer value"),"CanDeepToString(): failed to find expected string value in string output\n"+result);
            Assert.IsTrue(result.Contains("string,one.two"),"CanDeepToString(): Listvalue:DUMP isn't going deep enough to print inner member 'string,one.two'\n"+result);
            Assert.IsTrue(result.Contains("inner string 1"),"CanDeepToString(): Listvalue:DUMP isn't going deep enough to print inner member 'inner string 1'\n"+result);
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
