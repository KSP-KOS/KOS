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
            var expectedList = new List<string> { string.Empty, "ooBar", "ooBar" };
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
            var findChar = 0;
            var expectedIndex = "F";
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
    }
}