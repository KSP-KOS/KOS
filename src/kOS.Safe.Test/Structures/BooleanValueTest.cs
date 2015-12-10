using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Structures
{
    [TestFixture]
    public class BooleanValueTest
    {
        [Test]
        public void CanConstructFromBool()
        {
            bool b = true;
            BooleanValue bv = new BooleanValue(b);
            Assert.AreEqual(b, bv.Value);
            Assert.IsTrue(b == bv);
            Assert.IsFalse(b == !bv);
            Assert.IsFalse(b != bv);
            Assert.IsFalse(!b == bv);
        }

        [Test]
        public void CanImplicitlyConvertToBool()
        {
            bool b = true;
            BooleanValue bv = new BooleanValue(b);
            bool b2 = bv;
            Assert.AreEqual(b, b2);
        }

        [Test]
        public void CanAddAsString()
        {
            bool b = true;
            string string1 = "foo";
            var str1 = new StringValue(string1);
            var bv = new BooleanValue(b);
            var strResult = str1 + bv;
            string stringResult = string1 + b.ToString();
            Assert.AreEqual(stringResult, (string)strResult);
        }
    }
}
