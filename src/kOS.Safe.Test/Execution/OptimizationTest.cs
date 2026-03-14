using System;
using NUnit.Framework;
using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Test.Execution
{
    [TestFixture]
    public class OptimizationTest : BaseIntegrationTest
    {
        protected override OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;

        [Test]
        public void TestBasic()
        {
            // Tests that the bare minimum works

            RunScript("integration/basic.ks");
            RunSingleStep();
            AssertOutput(
                "text"
            );
        }

        [Test]
        public void TestVars()
        {
            // Tests that basic variable assignment and reference works

            RunScript("integration/vars.ks");
            RunSingleStep();
            AssertOutput(
                "1",
                "2",
                "3"
            );
        }

        [Test]
        public void TestFunc()
        {
            // Tests that basic no-args function calls work

            RunScript("integration/func.ks");
            RunSingleStep();
            AssertOutput(
                "a"
            );
        }

        [Test]
        public void TestFuncArgs()
        {
            // Tests that explicit and default function parameters work

            RunScript("integration/func_args.ks");
            RunSingleStep();
            AssertOutput(
                "0",
                "1",
                "2",
                "3"
            );
        }

        [Test]
        public void TestOperators()
        {
            // Test that all the basic operators work

            RunScript("integration/operators.ks");
            RunSingleStep();
            AssertOutput(
                "1",
                "1",
                "True",
                "True",
                "3",
                "6",
                "2",
                "9",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "True",
                "Ab",
                "0",
                "1",
                "0",
                "1"
            );
        }

        [Test]
        [ExpectedException(typeof(KOSCompileException))]
        public void TestOperatorsException()
        {
            // Test that an invalid operation throws a compile error during constant folding
            RunScript("integration/operators_invalid.ks");
            RunSingleStep();
        }

        [Test]
        public void TestLock()
        {
            // Test that locks in the same file works
            RunScript("integration/lock.ks");
            RunSingleStep();
            AssertOutput(
                "3",
                "4",
                "5"
            );
        }

        [Test]
        public void TestSuffixes()
        {
            // Test that various suffix and index combinations work for getting and setting
            RunScript("integration/suffixes.ks");
            RunSingleStep();
            RunSingleStep();
            AssertOutput(
                "0",
                "1",
                "2",
                "3",
                "0",
                "False"
            );
        }

        [Test]
        public void TestSuffixReplacement()
        {
            // Test that certain suffixes are replaced
            RunScript("integration/suffixReplacement.ks");
            RunSingleStep();
            AssertOutput(
                "9.80665",
                "3.14159265358979",
                "100"
            );
        }


        [Test]
        public void TestPeepholeOptimizations()
        {
            // Test that certain suffixes are replaced
            RunScript("integration/peepholeOptimizations.ks");
            RunSingleStep();
            AssertOutput(
                "String",
                "6",
                "6",
                "Print this.",
                "21",
                "21",
                "21",
                "21",
                "3",
                "-3",
                "7",
                "16",
                "125",
                "64",
                "3125",
                "256",
                "4",
                "25",
                "125",
                "125",
                "512"
            );
        }


        [Test]
        public void TestConstantPropagation()
        {
            // Test that local constant variables propagate
            RunScript("integration/constantPropagation.ks");
            RunSingleStep();
            AssertOutput(
                "test",
                "6",
                "False",
                "6",
                "9",
                "7",
                "10",
                "14",
                "True",
                "False",
                "7",
                "6",
                "11",
                "9",
                "5",
                "6",
                "1"
            );
        }
    }
}
