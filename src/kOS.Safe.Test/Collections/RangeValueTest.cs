using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;
using System.Linq;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class RangeValueTest : CollectionValueTest
    {
        [Test]
        public void CanCreate()
        {
            var range = new RangeValue();
            Assert.IsNotNull(range);
        }

        [Test]
        public void CanCreateEmptyRange()
        {
            var range = new RangeValue(0);
            Assert.AreEqual(ScalarIntValue.Zero, range.Count());
            Assert.IsTrue((BooleanValue)InvokeDelegate(range, "EMPTY"));
        }

        [Test]
        public void CanCreateSimpleRange()
        {
            var range = new RangeValue(5);
            Assert.AreEqual(new ScalarIntValue(5), InvokeDelegate(range, "LENGTH"));
            Assert.AreEqual(new ScalarIntValue(0), InvokeDelegate(range, "START"));
            Assert.AreEqual(new ScalarIntValue(5), InvokeDelegate(range, "STOP"));
            Assert.AreEqual(new ScalarIntValue(1), InvokeDelegate(range, "STEP"));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "EMPTY"));
            Assert.IsTrue((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(1)));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(5)));
        }

        [Test]
        public void CanCreateRangeWithFrom()
        {
            var range = new RangeValue(6, -3);
            Assert.AreEqual(new ScalarIntValue(9), InvokeDelegate(range, "LENGTH"));
            Assert.AreEqual(new ScalarIntValue(6), InvokeDelegate(range, "START"));
            Assert.AreEqual(new ScalarIntValue(-3), InvokeDelegate(range, "STOP"));
            Assert.AreEqual(new ScalarIntValue(1), InvokeDelegate(range, "STEP"));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "EMPTY"));
            Assert.IsTrue((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(-2)));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(-3)));
            Assert.IsTrue((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(6)));

            List<ScalarValue> l = range.ToList();
            Assert.AreEqual(new ScalarIntValue(6), l[0]);
            Assert.AreEqual(new ScalarIntValue(5), l[1]);
            Assert.AreEqual(new ScalarIntValue(-2), l[8]);
        }

        [Test]
        public void CanCreateRangeWithFromAndStep()
        {
            var range = new RangeValue(2, 12, 3);
            Assert.AreEqual(new ScalarIntValue(4), InvokeDelegate(range, "LENGTH"));
            Assert.AreEqual(new ScalarIntValue(2), InvokeDelegate(range, "START"));
            Assert.AreEqual(new ScalarIntValue(12), InvokeDelegate(range, "STOP"));
            Assert.AreEqual(new ScalarIntValue(3), InvokeDelegate(range, "STEP"));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "EMPTY"));
            Assert.IsTrue((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(5)));
            Assert.IsFalse((BooleanValue)InvokeDelegate(range, "CONTAINS", new ScalarIntValue(4)));

            List<ScalarValue> l = range.ToList();
            Assert.AreEqual(new ScalarIntValue(2), l[0]);
            Assert.AreEqual(new ScalarIntValue(5), l[1]);
            Assert.AreEqual(new ScalarIntValue(11), l[3]);
        }

        [Test]
        public void CanUseRangesWithValuesTooBigForInt32()
        {
            // This should be a range of values going [0, 1*max, 2*max, 3*max, 4*max]
            int oneMaxInt = Int32.MaxValue;
            long fiveTimesMaxInt = 5L * oneMaxInt;
            var range = new RangeValue(0, fiveTimesMaxInt, oneMaxInt);

            Assert.AreEqual(new ScalarIntValue(0), InvokeDelegate(range, "START"));
            Assert.AreEqual(new ScalarDoubleValue(fiveTimesMaxInt), InvokeDelegate(range, "STOP"));
            Assert.AreEqual(new ScalarIntValue(oneMaxInt), InvokeDelegate(range, "STEP"));

            List<ScalarValue> l = range.ToList();
            Assert.AreEqual(new ScalarIntValue(0), l[0]);
            Assert.AreEqual(new ScalarIntValue(oneMaxInt), l[1]);
            Assert.AreEqual((double)new ScalarDoubleValue(4d * oneMaxInt), (double)l[4]);
        }
    }
}
