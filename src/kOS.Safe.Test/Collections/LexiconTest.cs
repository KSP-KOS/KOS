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
            var lex = new Lexicon {{new StringValue("foo"), new StringValue("bar")}};

            var testValue = lex[new StringValue("foo")];

            Assert.AreEqual(new StringValue("bar"), testValue);
        }

        [Test]
        public void HasCaseInsensitiveKeys()
        {
            var lex = new Lexicon {{new StringValue("foo"), new StringValue("bar")}};

            Assert.AreEqual("bar", lex[new StringValue("FOO")]);
        }

        [Test]
        public void HashHitOnEqualValues()
        {
            var lex = new Lexicon {{ScalarDoubleValue.MaxValue(), new StringValue("bar")}};

            Assert.AreEqual("bar", lex[ScalarDoubleValue.MaxValue()]);
        }

        [Test]
        [ExpectedException(typeof(KOSKeyNotFoundException))]
        public void HashMissOnDifferentValues()
        {
            var lex = new Lexicon {{ScalarDoubleValue.MinValue(), new StringValue("bar")}};

            Assert.AreNotEqual("bar", lex[ScalarDoubleValue.MaxValue()]);
        }

        [Test]
        public void ContainsReturnsTrueIfTheKeyIsPresent()
        {
            var lex = new Lexicon {{ScalarDoubleValue.MinValue(), new StringValue("bar")}};

            Assert.IsTrue(lex.ContainsKey(ScalarDoubleValue.MinValue()));
        }

        [Test]
        public void ContainsReturnsFalseIfTheKeyIsMissing()
        {
            var lex = new Lexicon {{ScalarDoubleValue.MinValue(), new StringValue("bar")}};

            Assert.IsFalse(lex.ContainsKey(ScalarDoubleValue.MaxValue()));
        }

        [Test]
        public void CanRemoveKeyOfDifferentCase()
        {
            var lex = new Lexicon {{new StringValue("foo"), new StringValue("bar")}};

            Assert.AreEqual(1, lex.Count);

            lex.Remove(new StringValue("foo"));
            Assert.AreEqual(0, lex.Count);

            lex.Add(new StringValue("foo"), new StringValue("bar"));
            Assert.AreEqual(1, lex.Count);

            lex.Remove(new StringValue("FOO"));
            Assert.AreEqual(0, lex.Count);

            lex.Add(new StringValue("foo"), new StringValue("bar"));
            Assert.AreEqual(1, lex.Count);

            lex.Remove(new StringValue("Foo"));
            Assert.AreEqual(0, lex.Count);
        }

        [Test]
        public void DoesNotErrorOnRemoveNullKey()
        {
            var lex = new Lexicon();
            lex.Remove(new StringValue("foo"));
        }

        [Test]
        public void CanSetNewIndex()
        {
            var lex = new Lexicon();
            lex[new StringValue("foo")] = new StringValue("bang");

            Assert.AreEqual(1, lex.Count);
        }

        [Test]
        public void CanSetAndGetIndex()
        {
            var lex = new Lexicon();
            lex.SetIndex(new StringValue("fizz"), new StringValue("bang"));
            var value = lex.GetIndex(new StringValue("fizz"));

            Assert.AreEqual("bang", value);
        }

        [Test]
        [ExpectedException(typeof(KOSKeyNotFoundException))]
        public void ErrorsOnGetEmptyKey()
        {
            var lex = new Lexicon();
            var val = lex[new StringValue("fizz")];
        }

        [Test]
        public void CanDumpLexicon()
        {
            var map = MakeNestedExample();
            
            var result = (string)InvokeDelegate(map, "DUMP");
            
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
        public void CanFormatNumericKeys()
        {
            var map = MakeNestedExample();

            var hasKeyInner = (bool)InvokeDelegate(map, "HASKEY" , "inner");
            Assert.IsTrue(hasKeyInner);

            var inner = (Lexicon) ((Lexicon)map)[new StringValue("inner")];
            Assert.IsNotNull(inner);

            var hasNumericKey = (bool)InvokeDelegate(inner, "HASKEY" , 3);
            Assert.IsTrue(hasNumericKey);

            var innerString = inner.ToString();

            Assert.IsTrue(innerString.Contains("[\"2\"]"));
            Assert.IsTrue(innerString.Contains("[3]"));
        }

        [Test]
        public void CanClearOnCaseChange()
        {
            var map = MakeNestedExample();

            var length = (int)InvokeDelegate(map, "LENGTH");

            Assert.IsTrue(length > 0);

            map.SetSuffix("CASESENSITIVE", true);

            length = (int)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length == 0);

            InvokeDelegate(map,"ADD", "first", 100);

            length = (int)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length > 0);

            map.SetSuffix("CASESENSITIVE", false);

            length = (int)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length == 0);
        }

        private IDumper MakeNestedExample()
        {
            const string OUTER_STRING = "String, outer value";
            
            var map = new Lexicon();
            var innerMap1 = new Lexicon();
            var innerMap2 = new Lexicon();
            var innerInnerMap = new Lexicon
            {
                {new StringValue("inner"), new StringValue("inner string 1")}, 
                {new ScalarIntValue(2), new ScalarIntValue(2)}
            };

            innerMap1.Add(new StringValue("map"), innerInnerMap);
            innerMap1.Add(new StringValue("2"), new StringValue("string,one.two"));
            innerMap1.Add(new ScalarIntValue(3), new StringValue("string,one.three"));

            innerMap2.Add(new StringValue("testing"), new StringValue("string,two.one") );
            innerMap2.Add(new StringValue("2"), new StringValue("string,two.two") );
            
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
