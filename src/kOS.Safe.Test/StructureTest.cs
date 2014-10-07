using System;
using NSubstitute;
using NUnit.Framework;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class StructureTest
    {
        public class TestStructure: Structure
        {
            public static void TestAddGlobal<T>(string name, ISuffix suffix)
            {
                AddGlobalSuffix<T>(name,suffix);
            }

            public static void TestAddGlobal<T>(string[] name, ISuffix suffix)
            {
                AddGlobalSuffix<T>(name,suffix);
            }

            public void TestAddInstanceSuffix(string name, ISuffix suffix)
            {
                AddSuffix(name,suffix);
            }
        }

        [Test]
        public void CanConstruct()
        {
            var structure = new TestStructure();
            Assert.IsNotNull(structure);
        }
        [Test]
        public void CanAddGlobalSuffix()
        {
            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            var testStuffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            TestStructure.TestAddGlobal<TestStructure>(testStuffixName, testSuffix);
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(testStuffixName));
        }

        [Test]
        public void CanAddGlobalSuffixWithTwoNames()
        {
            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            var suffixName2 = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            TestStructure.TestAddGlobal<TestStructure>(new[]{suffixName,suffixName2}, testSuffix);
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName));
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName2));
        }

        [Test]
        public void GlobalSuffixesAreInFactGlobal()
        {
            var suffixName = Guid.NewGuid().ToString();
            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            testSuffix.Get().Returns(testObject);

            TestStructure.TestAddGlobal<TestStructure>(suffixName, testSuffix);
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(suffixName));
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(suffixName));
        }

        [Test]
        public void CanAddInstanceSuffix()
        {
            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void InstanceSuffixesAreInFactInstanced()
        {
            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);


            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName));
            Assert.AreEqual(null, new TestStructure().GetSuffix(suffixName));
        }
    }
}

