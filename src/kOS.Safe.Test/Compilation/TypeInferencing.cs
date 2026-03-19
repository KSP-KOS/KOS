using System;
using NUnit.Framework;
using kOS.Safe.Encapsulation;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Test.Compilation
{
    [TestFixture]
    public class TypeInferencing
    {
        [Test]
        public void StructureSuffixTest()
        {
            // Tests that the bare minimum works
            Type structureType = typeof(Encapsulation.Structure);
            Type result = TypeInferencer.GetTypeForSuffix(structureType, "TOSTRING");
            Assert.AreEqual(result, typeof(StringValue));

            Type integerType = typeof(ScalarIntValue);
            result = TypeInferencer.GetTypeForSuffix(integerType, "istype");
            Assert.AreEqual(result, typeof(BooleanValue));
        }

        [Test]
        public void ListSuffixTest()
        {
            // Test that lists and enumerables work
            Type listType = typeof(ListValue);
            Type result = TypeInferencer.GetTypeForSuffix(listType, "ITERATOR");
            Assert.AreEqual(result, typeof(Enumerator));

            result = TypeInferencer.GetTypeForSuffix(listType, "COPY");
            Assert.AreEqual(result, typeof(ListValue));

            result = TypeInferencer.GetTypeForIndex(listType);
            Assert.AreEqual(result, typeof(Encapsulation.Structure));

            result = TypeInferencer.GetTypeForSuffix(listType, "CLEAR");
            Assert.AreEqual(result, null);

            // Test an abstract generic type
            listType = typeof(EnumerableValue<Encapsulation.Structure, System.Collections.Generic.IEnumerable<Encapsulation.Structure>>);
            result = TypeInferencer.GetTypeForSuffix(listType, "ITERATOR");
            Assert.AreEqual(result, typeof(Enumerator));
        }
    }
}
