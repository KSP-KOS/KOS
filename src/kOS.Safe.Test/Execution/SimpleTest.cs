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
                "True"
            );
        }
    }
}
