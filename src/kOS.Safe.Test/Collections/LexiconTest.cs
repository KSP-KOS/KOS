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
            lex.SetIndex("fizz", "bang");
            var value = lex.GetIndex("fizz");

            Assert.AreEqual("bang", value);
        }

        [Test]
        [ExpectedException(typeof(KOSKeyNotFoundException))]
        public void ErrorsOnGetEmptyKey()
        {
            var lex = new Lexicon<object, object>();
            lex.GetIndex("fizz");
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidArgumentException))]
        public void ErrorsOnInvalidKeyType()
        {
            var lex = new Lexicon<double, object>();
            lex.GetIndex("fizz");
        }

        [Test]
        public void CanDumpLexicon()
        {
            var map = MakeNestedExample();
            
            string result = (string)InvokeDelegate(map, "DUMP");
            
            //TODO: build Asserts
        }

        [Test]
        public void CanPrintLexicon()
        {
            var map = MakeNestedExample();

            string result = map.ToString();

            //TODO: build Asserts
        }

        [Test]
        public void CanFindExistingKey()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(map, "HASKEY" , "first");
            Assert.IsTrue(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(map, "HASKEY" , "second");
            Assert.IsTrue(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(map, "HASKEY" , "second");
            Assert.IsTrue(hasKeyLast);
        }

        [Test]
        public void CantFindMissingKey()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(map, "HASKEY" , "2");
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(map, "HASKEY" , "3");
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(map, "HASKEY" , "testing");
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CanFindExistingValue()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(map, "HASVALUE" , 100);
            Assert.IsTrue(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(map, "HASVALUE" , 200);
            Assert.IsTrue(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(map, "HASVALUE" , "String, outer value");
            Assert.IsTrue(hasKeyLast);
        }

        [Test]
        public void CantFindMissingValue()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (bool)InvokeDelegate(map, "HASVALUE" , "2");
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (bool)InvokeDelegate(map, "HASVALUE" , "3");
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (bool)InvokeDelegate(map, "HASVALUE" , "testing");
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CopyGetsCollectionOfSameType()
        {
            var map = MakeNestedExample();

            var mapCopy = InvokeDelegate(map, "COPY");

            Assert.AreEqual(map.GetType(), mapCopy.GetType());
        }

        [Test]
        public void CopyIsDifferentObject()
        {
            var map = MakeNestedExample();
            var mapCopy = (IDumper)InvokeDelegate(map, "COPY");


            var hasKeyFirst = (bool)InvokeDelegate(map, "HASKEY" , "first");
            Assert.IsTrue(hasKeyFirst);
            InvokeDelegate(map, "REMOVE" , "first");
            var hasKeyFirstAfterRemove = (bool)InvokeDelegate(map, "HASKEY" , "first");
            Assert.IsFalse(hasKeyFirstAfterRemove);

            var copyHasKeyFirstAfterRemove = (bool)InvokeDelegate(mapCopy, "HASKEY" , "first");
            Assert.IsTrue(copyHasKeyFirstAfterRemove);

        }

        [Test]
        public void CanFormatNeumericKeys()
        {
            var map = MakeNestedExample();

            var hasKeyInner = (bool)InvokeDelegate(map, "HASKEY" , "inner");
            Assert.IsTrue(hasKeyInner);

            var inner = (Lexicon<object, object>) ((Lexicon<object,object>)map)["inner"];
            Assert.IsNotNull(inner);

            var hasNumericKey = (bool)InvokeDelegate(inner, "HASKEY" , 3);
            Assert.IsTrue(hasNumericKey);

            var innerString = inner.ToString();

            Assert.IsTrue(innerString.Contains("[\"2\"]"));
            Assert.IsTrue(innerString.Contains("[3]"));

        }

        private IDumper MakeNestedExample()
        {
            const string OUTER_STRING = "String, outer value";
            
            var map = new Lexicon<object,object>();
            var innerMap1 = new Lexicon<object,object>();
            var innerMap2 = new Lexicon<object,object>();
            var innerInnerMap = new Lexicon<object,object>
            {
                {"inner", "inner string 1"}, 
                {2, 2}
            };

            innerMap1.Add("map", innerInnerMap);
            innerMap1.Add("2", "string,one.two");
            innerMap1.Add(3, "string,one.three");

            innerMap2.Add("testing", "string,two.one" );
            innerMap2.Add("2", "string,two.two" );
            
            InvokeDelegate(map,"ADD", "first", 100);
            InvokeDelegate(map,"ADD", "second", 200);
            InvokeDelegate(map,"ADD", "inner", innerMap1);            
            InvokeDelegate(map,"ADD", "inner2", innerMap2);            
            InvokeDelegate(map,"ADD", "last", OUTER_STRING);
            
            return map;
        }

        private object InvokeDelegate(IDumper map, string suffixName, params object[] parameters)
        {
            var lengthObj = map.GetSuffix(suffixName);
            Assert.IsNotNull(lengthObj);
            var lengthDelegate = lengthObj as Delegate;
            Assert.IsNotNull(lengthDelegate);
            var toReturn = lengthDelegate.DynamicInvoke(parameters);
            return toReturn;
        }
    }
}
