using System;
using NUnit.Framework;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Encapsulation;
using kOS.Safe.Test.Execution;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test.Compilation
{

    [SetUpFixture]
    public class StaticSetup
    {
        [SetUp]
        public void Setup()
        {
            SafeHouse.Init(new Config(), new VersionInfo(0, 0, 0, 0), "", false, "./");
            SafeHouse.Logger = new NoopLogger();

            try
            {
                AssemblyWalkAttribute.Walk();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }
    }

    [TestFixture]
    public class TypeInferencing
    {
        [Test]
        public void StructureSuffixTest()
        {
            // Tests that the bare minimum works
            Type structureType = typeof(Encapsulation.Structure);
            Type result = TypeInferencer.GetTypeForSuffix(structureType, "TOSTRING");
            Assert.AreEqual(typeof(StringValue), result);

            Type integerType = typeof(ScalarIntValue);
            result = TypeInferencer.GetTypeForSuffix(integerType, "istype");
            Assert.AreEqual(typeof(BooleanValue), result);
        }

        [Test]
        public void ListSuffixTest()
        {
            // Test that lists and enumerables work
            Type listType = typeof(ListValue);
            Type result = TypeInferencer.GetTypeForSuffix(listType, "ITERATOR");
            Assert.AreEqual(typeof(Enumerator), result);

            result = TypeInferencer.GetTypeForSuffix(listType, "COPY");
            Assert.AreEqual(typeof(ListValue), result);

            result = TypeInferencer.GetTypeForIndex(listType);
            Assert.AreEqual(typeof(Encapsulation.Structure), result);

            result = TypeInferencer.GetTypeForSuffix(listType, "CLEAR");
            Assert.AreEqual(null, result);

            // Test an abstract generic type
            listType = typeof(EnumerableValue<Encapsulation.Structure, System.Collections.Generic.IEnumerable<Encapsulation.Structure>>);
            result = TypeInferencer.GetTypeForSuffix(listType, "ITERATOR");
            Assert.AreEqual(typeof(Enumerator), result);
        }

        [Test]
        public void TestInstructionInferencing()
        {
            InterimConstantValue a = new InterimConstantValue(ScalarIntValue.One, 0, 0);
            InterimConstantValue b = new InterimConstantValue(ScalarIntValue.Two, 0, 0);
            IRBinaryOp add = new IRBinaryOp(null, new OpcodeMathAdd(), a, b);

            Assert.AreEqual(typeof(ScalarValue), add.Type);

            IRCall call = new IRCall(null, new OpcodeCall("sin"), true, b);
            Assert.IsTrue(typeof(ScalarValue).IsAssignableFrom(call.Type));

            IRCall print = new IRCall(null, new OpcodeCall("print"), true, a);
            Assert.AreEqual(null, print.Type);

            IRCall userCall = new IRCall(null, new OpcodeCall("$test*"), true, a, b);
            Assert.AreEqual(typeof(Encapsulation.Structure), userCall.Type);
        }
    }
}
