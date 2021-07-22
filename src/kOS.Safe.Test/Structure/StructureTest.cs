using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using NSubstitute;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class StructureTest
    {
        [KOSNomenclature("StructureTest_TestStructure")]

        public class TestStructure: Encapsulation.Structure
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

        [TestFixtureSetUp]
        public void Setup()
        {
            SafeHouse.Logger = new TestLogger();
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
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            var testStuffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            TestStructure.TestAddGlobal<TestStructure>(testStuffixName, testSuffix);
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(testStuffixName));
        }

        [Test]
        public void CanAddGlobalSuffixWithTwoNames()
        {
            var testObject = Substitute.For<ISuffixResult>();
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
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            testSuffix.Get().Returns(testObject);

            TestStructure.TestAddGlobal<TestStructure>(suffixName, testSuffix);
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(suffixName));
            Assert.AreEqual(testObject, new TestStructure().GetSuffix(suffixName));
        }

        [Test]
        public void CanAddInstanceSuffix()
        {
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void CantFindSuffixThatDoesntExist()
        {
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            Assert.Throws(typeof(KOSSuffixUseException),
                          (() => testStructure.GetSuffix(suffixName)),
                          "failed to throw exception getting nonexistent suffix" );

            testStructure.TestAddInstanceSuffix(suffixName,testSuffix);
            Assert.IsNotNull(testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void CantFindStaticSuffixThatDoesntExist()
        {
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);

            Assert.Throws(typeof(KOSSuffixUseException),
                          (() => testStructure.GetSuffix(suffixName)),
                          "failed to throw exception getting nonexistent static suffix");
            
            TestStructure.TestAddGlobal<TestStructure>(suffixName,testSuffix);
            Assert.IsNotNull(testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void InstanceSuffixesAreInFactInstanced()
        {
            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            var testStructure = new TestStructure();
            var suffixName = Guid.NewGuid().ToString();
            testSuffix.Get().Returns(testObject);


            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);
            Assert.AreEqual(testObject, testStructure.GetSuffix(suffixName));
            Assert.Throws(typeof(KOSSuffixUseException),
                          (() => new TestStructure().GetSuffix(suffixName)),
                          "failed to throw exception getting instance suffix of unpopulated object");
                        
        }

        [Test]
        public void InstanceSuffixesAreCaseInsensitive()
        {
            var testStructure = new TestStructure();

            var testObject1 = Substitute.For<ISuffixResult>();
            var testSuffix1 = Substitute.For<ISuffix>();
            testSuffix1.Get().Returns(testObject1);

            var testObject2 = Substitute.For<ISuffixResult>();
            var testSuffix2 = Substitute.For<ISuffix>();
            testSuffix2.Get().Returns(testObject2);

            var suffixNameLower = Guid.NewGuid().ToString().ToLower();
            var suffixNameUpper = suffixNameLower.ToUpper();

            testStructure.TestAddInstanceSuffix(suffixNameUpper, testSuffix1);
            Assert.AreEqual(testObject1, testStructure.GetSuffix(suffixNameUpper));
            Assert.AreEqual(testObject1, testStructure.GetSuffix(suffixNameLower));
            testStructure.TestAddInstanceSuffix(suffixNameLower, testSuffix2);
            Assert.AreEqual(testObject2, testStructure.GetSuffix(suffixNameUpper));
            Assert.AreEqual(testObject2, testStructure.GetSuffix(suffixNameLower));
        }

        [Test]
        public void CanSetInstanceSuffix()
        {
            var testObject = Substitute.For<ISuffixResult>();

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

            var testObject = Substitute.For<ISuffixResult>();
            var testSuffix = Substitute.For<ISuffix>();
            testSuffix.Get().ReturnsForAnyArgs(info => testObject);
            testStructure.TestAddInstanceSuffix(suffixName, testSuffix);

            var testSuffixStatic = Substitute.For<ISuffix>();
            testSuffixStatic.Get().ReturnsForAnyArgs(new SuffixResult(ScalarIntValue.MaxValue()));
            TestStructure.TestAddGlobal<object>(suffixName, testSuffixStatic);

            Assert.IsNotNull(testStructure.GetSuffix(suffixName));
            Assert.AreSame(testObject,testStructure.GetSuffix(suffixName));
        }

        [Test]
        public void CanSetSynonymInstanceSuffix()
        {
            var testObject = Substitute.For<ISuffixResult>();
            var testObject2 = Substitute.For<ISuffixResult>();

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

