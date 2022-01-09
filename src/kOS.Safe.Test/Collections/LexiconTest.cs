using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Safe.Test.Opcode;
using NUnit.Framework;
using kOS.Safe.Serialization;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class LexiconTest
    {
        private ICpu cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new FakeCpu();
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

            Assert.AreEqual(new StringValue("bar"), lex[new StringValue("FOO")]);
        }

        [Test]
        public void HashHitOnEqualValues()
        {
            var lex = new Lexicon {{ScalarDoubleValue.MaxValue(), new StringValue("bar")}};

            Assert.AreEqual(new StringValue("bar"), lex[ScalarDoubleValue.MaxValue()]);
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

            Assert.AreEqual(new StringValue("bang"), value);
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
            
            var result = (StringValue)InvokeDelegate(map, "DUMP");
            
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

            var hasKeyFirst = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("first"));
            Assert.IsTrue(hasKeyFirst);
            var hasKeySecond = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("second"));
            Assert.IsTrue(hasKeySecond);
            var hasKeyLast = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("second"));
            Assert.IsTrue(hasKeyLast);
        }

        [Test]
        public void CantFindMissingKey()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("2"));
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("3"));
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("testing"));
            Assert.IsFalse(hasKeyLast);
        }

        [Test]
        public void CanFindExistingValue()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new ScalarIntValue(100));
            Assert.IsTrue(hasKeyFirst);
            var hasKeySecond = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new ScalarIntValue(200));
            Assert.IsTrue(hasKeySecond);
            var hasKeyLast = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new StringValue("String, outer value"));
            Assert.IsTrue(hasKeyLast);
        }

        [Test]
        public void CantFindMissingValue()
        {
            var map = MakeNestedExample();

            var hasKeyFirst = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new StringValue("2"));
            Assert.IsFalse(hasKeyFirst);
            var hasKeySecond = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new StringValue("3"));
            Assert.IsFalse(hasKeySecond);
            var hasKeyLast = (BooleanValue)InvokeDelegate(map, "HASVALUE" , new StringValue("testing"));
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
            var mapCopy = (Lexicon)InvokeDelegate(map, "COPY");

            var hasKeyFirst = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("first"));
            Assert.IsTrue(hasKeyFirst);
            InvokeDelegate(map, "REMOVE" , new StringValue("first"));
            var hasKeyFirstAfterRemove = (BooleanValue)InvokeDelegate(map, "HASKEY" , new StringValue("first"));
            Assert.IsFalse(hasKeyFirstAfterRemove);

            var copyHasKeyFirstAfterRemove = (BooleanValue)InvokeDelegate(mapCopy, "HASKEY" , new StringValue("first"));
            Assert.IsTrue(copyHasKeyFirstAfterRemove);

        }

        [Test]
        public void CanClearOnCaseChange()
        {
            var map = MakeNestedExample();

            var length = (ScalarIntValue)InvokeDelegate(map, "LENGTH");

            Assert.IsTrue(length > 0);

            map.SetSuffix("CASESENSITIVE", BooleanValue.True);

            length = (ScalarIntValue)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length == 0);

            InvokeDelegate(map,"ADD", new StringValue("first"), new ScalarIntValue(100));

            length = (ScalarIntValue)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length > 0);

            map.SetSuffix("CASESENSITIVE", BooleanValue.False);

            length = (ScalarIntValue)InvokeDelegate(map, "LENGTH");
            Assert.IsTrue(length == 0);
        }

        private Lexicon MakeNestedExample()
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
            
            InvokeDelegate(map,"ADD", new StringValue("first"), new ScalarIntValue(100));
            InvokeDelegate(map,"ADD", new StringValue("second"), new ScalarIntValue(200));
            InvokeDelegate(map,"ADD", new StringValue("inner"), innerMap1);
            InvokeDelegate(map,"ADD", new StringValue("inner2"), innerMap2);
            InvokeDelegate(map,"ADD", new StringValue("last"), new StringValue(OUTER_STRING));
            
            return map;
        }

        private Encapsulation.Structure InvokeDelegate(Lexicon map, string suffixName, params Encapsulation.Structure[] parameters)
        {
            ISuffixResult lengthResult = map.GetSuffix(suffixName);
            Assert.IsNotNull(lengthResult);

            if (!lengthResult.HasValue)
            {
                var delegateResult = lengthResult as DelegateSuffixResult;
                if (delegateResult != null)
                {
                    cpu.PushArgumentStack(null); // fake delegate info
                    cpu.PushArgumentStack(new KOSArgMarkerType());
                    foreach (var param in parameters)
                    {
                        cpu.PushArgumentStack(param);
                    }

                    delegateResult.Invoke(cpu);
                    
                    return delegateResult.Value;
                }
            }

            return lengthResult.Value;
        }
    }
}
