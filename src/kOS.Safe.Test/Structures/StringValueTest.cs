using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Structures
{
    [TestFixture]
    public class StringValueTest
    {
        [Test]
        public void CanMakeEmpty()
        {
            var sv = new StringValue();

            Assert.AreEqual(string.Empty, sv.ToString());
        }

        [Test]
        public void CanToString()
        {
            var testValue = "foobar";
            var sv = new StringValue(testValue);

            Assert.AreEqual(testValue, sv.ToString());
        }

        [Test]
        public void CanPreserveCase()
        {
            var testValue = "FooBar";
            var sv = new StringValue(testValue);

            Assert.AreEqual(testValue, sv.ToString());
        }

        [Test]
        public void CanStartsWith()
        {
            var testValue = "FooBar";
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.IsTrue(sv.StartsWith(testValue));

            //Case Insensitive
            Assert.IsTrue(sv.StartsWith(testValue.ToLower()));
            Assert.IsTrue(sv.StartsWith(testValue.ToUpper()));
        }

        [Test]
        public void CanEndsWith()
        {
            var testValue = "FooBar";
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.IsTrue(sv.EndsWith(testValue));

            //Case Insensitive
            Assert.IsTrue(sv.EndsWith(testValue.ToLower()));
            Assert.IsTrue(sv.EndsWith(testValue.ToUpper()));
        }

        [Test]
        public void CanContains()
        {
            var testValue = "FooBar";
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.IsTrue(sv.Contains(testValue));

            //Case Insensitive
            Assert.IsTrue(sv.Contains(testValue.ToLower()));
            Assert.IsTrue(sv.Contains(testValue.ToUpper()));
        }

        [Test]
        public void CanFindAt()
        {
            var testValue = "FooBarFooBar";
            var findChar = "F";
            var expectedIndex = 6;
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.AreEqual(expectedIndex, sv.FindAt(findChar, 4));

            //Case Insensitive
            Assert.AreEqual(expectedIndex, sv.FindAt(findChar.ToLower(), 4));
        }

        [Test]
        public void CanFindLastAt()
        {
            var testValue = "FooBarFooBar";
            var findChar = "F";
            var expectedIndex = 6;
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.AreEqual(expectedIndex, sv.FindLastAt(findChar, 8));

            //Case Insensitive
            Assert.AreEqual(expectedIndex, sv.FindLastAt(findChar.ToLower(), 8));
        }

        [Test]
        public void CanSplit()
        {
            var testValue = "FooBarFooBar";
            var findChar = "F";
            var expectedList = new List<StringValue> { new StringValue(string.Empty), new StringValue("ooBar"), new StringValue("ooBar") };
            var sv = new StringValue(testValue);

            //Case Sensitive
            CollectionAssert.AreEqual(expectedList, sv.SplitToList(findChar));

            //Case Insensitive
            CollectionAssert.AreEqual(expectedList, sv.SplitToList(findChar.ToLower()));
        }

        [Test]
        public void CanIndexOf()
        {
            var testValue = "FooBarFooBar";
            var findChar = "F";
            var expectedIndex = 0;
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.AreEqual(expectedIndex, sv.IndexOf(findChar));

            //Case Insensitive
            Assert.AreEqual(expectedIndex, sv.IndexOf(findChar.ToLower()));
        }

        [Test]
        public void CanGetIndex()
        {
            var testValue = "FooBarFooBar";
            var expectedIndex = new StringValue("F");
            var sv = new StringValue(testValue);

            //Case Sensitive
            Assert.AreEqual(expectedIndex, sv.GetIndex(0));
        }

        [Test]
        public void CanNullCheck()
        {
            StringValue testValue = null;
            Assert.IsTrue(testValue == null);
            Assert.IsFalse(testValue != null);
            Assert.IsTrue(null == testValue);
            Assert.IsFalse(null != testValue);
            Assert.AreEqual(testValue, null);
            Assert.AreEqual(null, testValue);
            testValue = new StringValue("FooBar");
            Assert.IsTrue(testValue != null);
            Assert.IsFalse(testValue == null);
            Assert.IsTrue(null != testValue);
            Assert.IsFalse(null == testValue);
            Assert.AreNotEqual(testValue, null);
            Assert.AreNotEqual(null, testValue);
        }

        [Test]
        public void CanParseScientificStrings()
        {
            // check using a double value
            ScalarValue doubleTest = 1.23e-5;
            Assert.IsTrue(doubleTest.IsDouble);
            var stringTest = new StringValue("1.23e-5");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);
            stringTest = new StringValue("1.23 e -5");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);
            stringTest = new StringValue("1.23e -5");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);
            stringTest = new StringValue("1.23 e-5");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);
            stringTest = new StringValue(" 1.23 e -5 ");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);
            stringTest = new StringValue(" 1.23e-5 ");
            Assert.IsTrue(stringTest.ToScalar() == doubleTest);

            // check using an integer value
            ScalarValue intTest = 1.23e3;
            Assert.IsTrue(intTest.IsInt);
            // without sign symbol
            stringTest = new StringValue("1.23e3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23 e 3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23e 3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23 e3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue(" 1.23 e 3 ");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue(" 1.23e3 ");
            Assert.IsTrue(stringTest.ToScalar() == intTest);

            // with sign symbol
            stringTest = new StringValue("1.23e+3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23 e +3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23e +3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue("1.23 e+3");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue(" 1.23 e +3 ");
            Assert.IsTrue(stringTest.ToScalar() == intTest);
            stringTest = new StringValue(" 1.23e+3 ");
            Assert.IsTrue(stringTest.ToScalar() == intTest);

            // test error throwing with invalid format
            stringTest = new StringValue(" 1.23e+3a ");
            Assert.Throws(typeof(Exceptions.KOSNumberParseException), () => stringTest.ToScalar());
        }

        [Test]
        public void CanFormat()
        {
            var formatString = new StringValue("test1={0:0.0} test2='{0,10:0.0}'");

            var expected = new StringValue("test1=13.4 test2='      13.4'");
            var actual = formatString.Format(new ScalarDoubleValue(13.37));

            Assert.That(expected.ToString(), Is.EqualTo(actual.ToString()));
        }
    }
}