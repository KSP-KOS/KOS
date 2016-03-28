using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Calculator
{
    [TestFixture]
    public class CalculatorStructureTest
    {
        CalculatorStructure calc;
        [SetUp]
        public void Setup()
        {
            calc = new CalculatorStructure();
        }

        [Test]
        public void CanEqualsPIDLoop()
        {
            var pidLoop1 = new PIDLoop();
            var pidLoop2 = new PIDLoop();

            var op = new OperandPair(pidLoop1, pidLoop2);
            var result = calc.Equal(op);
            Assert.IsFalse((bool)result);

            op = new OperandPair(pidLoop1, pidLoop1);
            result = calc.Equal(op);
            Assert.IsTrue((bool)result);
        }
    }
}
