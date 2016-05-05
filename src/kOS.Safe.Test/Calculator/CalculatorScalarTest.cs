using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Test.Opcode;
using NUnit.Framework;
using kOS.Safe.Serialization;
using kOS.Safe.Compilation;

namespace kOS.Safe.Test.Calculator
{
    [TestFixture]
    public class CalculatorScalarTest
    {
        CalculatorScalar calc;
        [SetUp]
        public void Setup()
        {
            calc = new CalculatorScalar();
        }

        [Test]
        public void CanAddIntegers()
        {
            var scalar1 = ScalarValue.Create(1);
            var scalar2 = ScalarValue.Create(1);

            var op = new OperandPair(scalar1, scalar2);
            var result = calc.Add(op);
            Assert.IsInstanceOf<ScalarValue>(result);

            var expected = scalar1 + scalar2;
            Assert.AreEqual(expected, result);
        }
    }
}
