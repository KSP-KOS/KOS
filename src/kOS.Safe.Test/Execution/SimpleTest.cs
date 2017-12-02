using System;
using NUnit.Framework;

namespace kOS.Safe.Test.Execution
{
    [TestFixture]
    public class SimpleTest : BaseIntegrationTest
    {

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
                "a",
                "b"
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
                "True"
            );
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
        public void TestShortCircuit()
        {
            // Test that boolean logic short circuits
            RunScript("integration/short_circuit.ks");
            RunSingleStep();
            AssertOutput(
                "a",
                // short circuit away b
                "False",
                "a",
                "b",
                "True",
                "b",
                "a",
                "False",
                "b",
                // short circuit away a
                "True"
            );
        }
    }
}
