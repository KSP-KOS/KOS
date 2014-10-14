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

            public void TestAddInstanceSuffix(string[] name, ISuffix suffix)
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

        [Test]
        public void CanSetInstanceSuffix()
        {
            var testObject = new object();

            object internalStorage = null;
            var testSuffix = Substitute.For<ISetSuffix>();
            //Mock Set
            testSuffix.When(s => s.Set(Arg.Any<object>())).Do(info => internalStorage = info.Arg<object>());
            //Mock Get
            testSuffix.Get().ReturnsForAnyArgs(info => internalStorage);

            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();

            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);

            Assert.IsNull(testStructure.GetSuffix(suffixName));

            Assert.IsTrue(testStructure.SetSuffix(suffixName, testObject));
            Assert.AreSame(testObject, testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void CanLetInstanceTakePrecedenceOverStatic()
        {
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();

            var testObject = new object();
            var testSuffix = Substitute.For<ISuffix>();
            testSuffix.Get().ReturnsForAnyArgs(info => testObject);
            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);

            var testSuffixStatic = Substitute.For<ISuffix>();
            testSuffixStatic.Get().ReturnsForAnyArgs(info => int.MaxValue);
            TestStructure.TestAddGlobal<object>(suffixName, testSuffixStatic);

            Assert.IsNotNull(testStructure.GetSuffix(suffixName));
            Assert.AreSame(testObject,testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void CanSetSynonymInstanceSuffix()
        {
            var testObject = new object();
            var testObject2 = new object();

            object internalStorage = null;
            var testSuffix = Substitute.For<ISetSuffix>();
            //Mock Set
            testSuffix.When(s => s.Set(Arg.Any<object>())).Do(info => internalStorage = info.Arg<object>());
            //Mock Get
            testSuffix.Get().ReturnsForAnyArgs(info => internalStorage);

            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            var suffixName2 = Guid.NewGuid().ToString();

            testStructure.TestAddInstanceSuffix(new[]{suffixName,suffixName2}, testSuffix);

            Assert.IsNull(testStructure.GetSuffix(suffixName));
            Assert.IsNull(testStructure.GetSuffix(suffixName2));

            testStructure.SetSuffix(suffixName, testObject);
            Assert.AreSame(testObject, testStructure.GetSuffix(suffixName));
            Assert.AreSame(testObject, testStructure.GetSuffix(suffixName2));

            testStructure.SetSuffix(suffixName2, testObject2);
            Assert.AreSame(testObject2, testStructure.GetSuffix(suffixName));
            Assert.AreSame(testObject2, testStructure.GetSuffix(suffixName2));
        }

    }
}

