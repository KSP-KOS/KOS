using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Structures
{
    [TestFixture]
    public class ScalarValueTest
    {
        [Test]
        public void CanConvertInt()
        {
            int i = 10;
            var s = ScalarValue.Create(i);
            Assert.IsTrue(s.IsInt);
            Assert.IsFalse(s.IsDouble);
            Assert.AreEqual(i, (int)s);
        }

        [Test]
        public void CanConvertDouble()
        {
            double d = 3.1415926535897932384626433832795;
            var s = ScalarValue.Create(d);
            Assert.IsFalse(s.IsInt);
            Assert.IsTrue(s.IsDouble);
            Assert.AreEqual(d, (double)s);
        }

        [Test]
        public void CanStructureFromPrimitiveInt()
        {
            int i = 10;
            var s = kOS.Safe.Encapsulation.Structure.FromPrimitive(i);
            Assert.AreEqual(i, (ScalarValue)s);
        }

        [Test]
        public void CanStructureFromPrimitiveDouble()
        {
            double d = 3.1415926535897932384626433832795;
            var s = kOS.Safe.Encapsulation.Structure.FromPrimitive(d);
            Assert.AreEqual(d, (ScalarValue)s, 0d);
        }

        [Test]
        public void CanStructureToPrimitiveInt()
        {
            int i = 10;
            var s = ScalarValue.Create(i);
            var iTest = kOS.Safe.Encapsulation.Structure.ToPrimitive(s);
            Assert.AreEqual(i, (int)iTest);
        }

        [Test]
        public void CanStructureToPrimitiveDouble()
        {
            double d = 3.1415926535897932384626433832795;
            var s = ScalarValue.Create(d);
            var dTest = kOS.Safe.Encapsulation.Structure.ToPrimitive(s);
            Assert.AreEqual(d, (double)dTest, 0d);
        }

        [Test]
        public void CanImplicitCastToInt()
        {
            int i = 10;
            var s = ScalarValue.Create(i);
            int iTest = s;
            Assert.AreEqual(i, iTest);
        }

        [Test]
        public void CanImplicitCastToDouble()
        {
            double d = 3.1415926535897932384626433832795;
            var s = ScalarValue.Create(d);
            double dTest = s;
            Assert.AreEqual(d, dTest);
        }

        [Test]
        public void CanImplicitCastFromInt()
        {
            int i = 10;
            var s = ScalarValue.Create(i);
            ScalarValue sTest = i;
            Assert.AreEqual(s, sTest);
        }

        [Test]
        public void CanImplicitCastFromDouble()
        {
            double d = 3.1415926535897932384626433832795;
            var s = ScalarValue.Create(d);
            ScalarValue sTest = d;
            Assert.AreEqual(s, sTest);
        }

        [Test]
        public void CanAdd()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = d1 + d2;
            ScalarValue sResult = s1 + s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanAddMixedType()
        {
            double d1 = 10.5;
            int i2 = 5;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(i2);
            double dResult = d1 + i2;
            ScalarValue sResult = s1 + s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanSubtract()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = d1 - d2;
            ScalarValue sResult = s1 - s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanMultiply()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = d1 * d2;
            ScalarValue sResult = s1 * s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanDivide()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = d1 / d2;
            ScalarValue sResult = s1 / s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanPower()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = Math.Pow(d1, d2);
            ScalarValue sResult = s1 ^ s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanModulus()
        {
            double d1 = 10.5;
            double d2 = 5.25;
            var s1 = ScalarValue.Create(d1);
            var s2 = ScalarValue.Create(d2);
            double dResult = d1 % d2;
            ScalarValue sResult = s1 % s2;
            Assert.AreEqual(dResult, sResult, 0d);
        }

        [Test]
        public void CanAddAsString()
        {
            double d1 = 10.5;
            string string1 = "foo";
            var str1 = new StringValue(string1);
            var s1 = ScalarValue.Create(d1);
            var strResult = str1 + s1;
            string stringResult = string1 + d1.ToString();
            Assert.AreEqual(stringResult, (string)strResult);
        }

        [Test]
        public void CanNullCheck()
        {
            ScalarValue sv = null;
            Assert.IsTrue(sv == null);
            Assert.IsFalse(sv != null);
            sv = ScalarValue.Create(1);
            Assert.IsTrue(sv != null);
            Assert.IsFalse(sv == null);
            Assert.IsTrue(null != sv);
            Assert.IsFalse(null == sv);
            sv = ScalarValue.Create(0);
            Assert.IsTrue(sv != null);
            Assert.IsFalse(sv == null);
            Assert.IsTrue(null != sv);
            Assert.IsFalse(null == sv);
            sv = ScalarValue.Create(3.1415926535897932384626433832795);
            Assert.IsTrue(sv != null);
            Assert.IsFalse(sv == null);
            Assert.IsTrue(null != sv);
            Assert.IsFalse(null == sv);
        }
    }
}
