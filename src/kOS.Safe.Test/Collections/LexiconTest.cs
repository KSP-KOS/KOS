using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class LexiconTest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void CanAddAndGetKey()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");
            var testValue = lex["foo"];

            Assert.AreEqual("bar", testValue);
        }

        [Test]
        public void HasCaseInsensitiveKeys()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");

            Assert.AreEqual("bar", lex["FOO"]);
        }

        [Test]
        public void HashHitOnEqualValues()
        {
            var lex = new Lexicon<double, object>();
            
            lex.Add(double.MaxValue, "bar");

            Assert.AreEqual("bar", lex[double.MaxValue]);
        }

        [Test]
        [ExpectedException(typeof(KOSKeyNotFoundException))]
        public void HashMissOnDifferentValues()
        {
            var lex = new Lexicon<double, object>();
            
            lex.Add(double.MinValue, "bar");

            Assert.AreNotEqual("bar", lex[double.MaxValue]);
        }

        [Test]
        public void ContainsReturnsTrueIfTheKeyIsPresent()
        {
            var lex = new Lexicon<double, object>();
            
            lex.Add(double.MinValue, "bar");

            Assert.IsTrue(lex.ContainsKey(double.MinValue));
        }

        [Test]
        public void ContainsReturnsFalseIfTheKeyIsMissing()
        {
            var lex = new Lexicon<double, object>();
            
            lex.Add(double.MinValue, "bar");

            Assert.IsFalse(lex.ContainsKey(double.MaxValue));
        }

        [Test]
        public void WillReplaceWithDifferentCase()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");
            Assert.AreEqual("bar", lex["foo"]);
            Assert.AreEqual("bar", lex["FOO"]);
            Assert.AreEqual("bar", lex["Foo"]);

            lex.Add("FOO", "fizz");
            Assert.AreEqual("fizz", lex["foo"]);
            Assert.AreEqual("fizz", lex["FOO"]);
            Assert.AreEqual("fizz", lex["Foo"]);

            lex.Add("Foo", "bang");
            Assert.AreEqual("bang", lex["foo"]);
            Assert.AreEqual("bang", lex["FOO"]);
            Assert.AreEqual("bang", lex["Foo"]);
        }

        [Test]
        public void CanRemoveKeyOfDifferentCase()
        {
            var lex = new Lexicon<object, object>();
            
            lex.Add("foo", "bar");
            Assert.AreEqual(1, lex.Count);

            lex.Remove("foo");
            Assert.AreEqual(0, lex.Count);

            lex.Add("foo", "bar");
            Assert.AreEqual(1, lex.Count);

            lex.Remove("FOO");
            Assert.AreEqual(0, lex.Count);

            lex.Add("foo", "bar");
            Assert.AreEqual(1, lex.Count);

            lex.Remove("Foo");
            Assert.AreEqual(0, lex.Count);
        }

        [Test]
        public void DoesNotErrorOnRemoveNullKey()
        {
            var lex = new Lexicon<object, object>();
            lex.Remove("foo");
        }

        [Test]
        public void CanSetNewIndex()
        {
            var lex = new Lexicon<object, object>();
            lex["foo"] = "bang";

            Assert.AreEqual(1, lex.Count);
        }

        [Test]
        public void CanSetAndGetIndex()
        {
            var lex = new Lexicon<object, object>();
            lex.SetKey("fizz", "bang");
            var value = lex.GetKey("fizz");

            Assert.AreEqual("bang", value);
        }

        [Test]
        [ExpectedException(typeof(KOSKeyNotFoundException))]
        public void ErrorsOnGetEmptyKey()
        {
            var lex = new Lexicon<object, object>();
            lex.GetKey("fizz");
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidArgumentException))]
        public void ErrorsOnInvalidKeyType()
        {
            var lex = new Lexicon<double, object>();
            lex.GetKey("fizz");
        }

        [Test]
        public void CanDumpLexicon()
        {
            var list = MakeNestedExample();
            
            string result = (string)InvokeDelegate(list, "DUMP");
            
            //TODO: build Asserts
        }

        [Test]
        public void CanPrintLexicon()
        {
            var list = MakeNestedExample();

            string result = list.ToString();

            //TODO: build Asserts
        }

        [Test]
        public void CanFindExistingKey()
        {
            var list = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(list, "HASKEY" , "first");
            Assert.IsTrue(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(list, "HASKEY" , "second");
            Assert.IsTrue(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(list, "HASKEY" , "second");
            Assert.IsTrue(hasKeyLast);
        }

        [Test]
        public void CantFindMissingKey()
        {
            var list = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(list, "HASKEY" , "2");
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(list, "HASKEY" , "3");
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(list, "HASKEY" , "testing");
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CanFindExistingValue()
        {
            var list = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(list, "HASKEY" , 100);
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(list, "HASKEY" , 200);
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(list, "HASKEY" , "String, outer value");
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CantFindMissingValue()
        {
            var list = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(list, "HASKEY" , "2");
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(list, "HASKEY" , "3");
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(list, "HASKEY" , "testing");
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CanCopy()
        {
            var list = MakeNestedExample();

            var listCopy = InvokeDelegate(list, "COPY");

            Assert.AreEqual(list.GetType(), listCopy.GetType());
        }

        [Test]
        public void CopyIsDifferentObject()
        {
            var list = MakeNestedExample();
            var listCopy = (IDumper)InvokeDelegate(list, "COPY");


            var hasKeyFirst = (bool)InvokeDelegate(list, "HASKEY" , "first");
            Assert.IsTrue(hasKeyFirst);
            InvokeDelegate(list, "REMOVE" , "first");
            var hasKeyFirstAfterRemove = (bool)InvokeDelegate(list, "HASKEY" , "first");
            Assert.IsFalse(hasKeyFirstAfterRemove);

            var copyHasKeyFirstAfterRemove = (bool)InvokeDelegate(listCopy, "HASKEY" , "first");
            Assert.IsTrue(copyHasKeyFirstAfterRemove);

        }


        private IDumper MakeNestedExample()
        {
            const string OUTER_STRING = "String, outer value";
            
            var list = new Lexicon<object,object>();
            var innerList1 = new Lexicon<object,object>();
            var innerList2 = new Lexicon<object,object>();
            var innerInnerList = new Lexicon<object,object>
            {
                {"inner", "inner string 1"}, 
                {2, 2}
            };

            innerList1.Add("list", innerInnerList);
            innerList1.Add("2", "string,one.two");
            innerList1.Add("3", "string,one.three");

            innerList2.Add("testing", "string,two.one" );
            innerList2.Add("2", "string,two.two" );
            
            InvokeDelegate(list,"ADD", "first", 100);
            InvokeDelegate(list,"ADD", "second", 200);
            InvokeDelegate(list,"ADD", "inner", innerList1);            
            InvokeDelegate(list,"ADD", "inner2", innerList2);            
            InvokeDelegate(list,"ADD", "last", OUTER_STRING);
            
            return list;
        }

        private object InvokeDelegate(IDumper list, string suffixName, params object[] parameters)
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
