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
            Assert.IsTrue(b == bv);
            Assert.IsFalse(b != bv);
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

        [Test]
        public void CanNullCheck()
        {
            BooleanValue bv = null;
            Assert.IsTrue(bv == null);
            Assert.IsFalse(bv != null);
            Assert.IsTrue(null == bv);
            Assert.IsFalse(null != bv);
            bv = new BooleanValue(true);
            Assert.IsTrue(bv != null);
            Assert.IsFalse(bv == null);
            Assert.IsTrue(null != bv);
            Assert.IsFalse(null == bv);
            bv = new BooleanValue(false);
            Assert.IsTrue(bv != null);
            Assert.IsFalse(bv == null);
            Assert.IsTrue(null != bv);
            Assert.IsFalse(null == bv);
            
            bv = new BooleanValue(true);
            Assert.IsTrue(bv != null);
            Assert.IsFalse(bv == null);
            Assert.IsTrue(null != bv);
            Assert.IsFalse(null == bv);
            bv = new BooleanValue(false);
            Assert.IsTrue(bv != null);
            Assert.IsFalse(bv == null);
            Assert.IsTrue(null != bv);
            Assert.IsFalse(null == bv);
        }

        [Test]
        public void CanCompareToScalar()
        {
            BooleanValue bv = new BooleanValue(true);
            ScalarValue sv = ScalarValue.Create(1);
            Assert.IsTrue(bv == sv);
            Assert.IsFalse(bv != sv);
            Assert.IsTrue(sv == bv);
            Assert.IsFalse(sv != bv);
            sv = ScalarValue.Create(0);
            Assert.IsTrue(bv != sv);
            Assert.IsFalse(bv == sv);
            Assert.IsTrue(sv != bv);
            Assert.IsFalse(sv == bv);
            sv = ScalarValue.Create(3.1415926535897932384626433832795);
            Assert.IsTrue(bv == sv);
            Assert.IsFalse(bv != sv);
            Assert.IsTrue(sv == bv);
            Assert.IsFalse(sv != bv);
            sv = ScalarValue.Create(0.0d);
            Assert.IsTrue(bv != sv);
            Assert.IsFalse(bv == sv);
            Assert.IsTrue(sv != bv);
            Assert.IsFalse(sv == bv);

            bv = new BooleanValue(false);
            sv = ScalarValue.Create(1);
            Assert.IsTrue(bv != sv);
            Assert.IsFalse(bv == sv);
            Assert.IsTrue(sv != bv);
            Assert.IsFalse(sv == bv);
            sv = ScalarValue.Create(0);
            Assert.IsTrue(bv == sv);
            Assert.IsFalse(bv != sv);
            Assert.IsTrue(sv == bv);
            Assert.IsFalse(sv != bv);
            sv = ScalarValue.Create(3.1415926535897932384626433832795);
            Assert.IsTrue(bv != sv);
            Assert.IsFalse(bv == sv);
            Assert.IsTrue(sv != bv);
            Assert.IsFalse(sv == bv);
            Assert.IsFalse(bv.Equals(sv));
            sv = ScalarValue.Create(0.0d);
            Assert.IsTrue(bv == sv);
            Assert.IsFalse(bv != sv);
            Assert.IsTrue(sv == bv);
            Assert.IsFalse(sv != bv);
            Assert.IsFalse(bv.Equals(sv));
        }
    }
}
