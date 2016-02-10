using System.Runtime.CompilerServices;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class StructureSuffixIntegrationTest
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            SafeHouse.Logger = new TestLogger();
        }

        [Test]
        public void CanAddSuffix()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<BooleanValue>(false);
            structure.TestAddInstanceSuffix("FOO", new Suffix<BooleanValue>(BuildBasicGetter(strongBox)));
            Assert.IsFalse((BooleanValue) structure.GetSuffix("FOO").Value);
        }

        [Test]
        public void CanReflectChangeInUnderlyingData()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<BooleanValue>(false);
            structure.TestAddInstanceSuffix("FOO", new Suffix<BooleanValue>(BuildBasicGetter(strongBox)));

            strongBox.Value = false;
            Assert.IsFalse((BooleanValue) structure.GetSuffix("FOO").Value);
            strongBox.Value = true;
            Assert.IsTrue((BooleanValue) structure.GetSuffix("FOO").Value);
        }

        [Test]
        public void CanAddSetSuffix()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<BooleanValue>(false);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<BooleanValue>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

            Assert.IsFalse((BooleanValue) structure.GetSuffix("FOO").Value);
        }

        [Test]
        public void CanSetSuffix()
        {
            const int TEST_VALUE = 12345;
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<ScalarIntValue>(TEST_VALUE);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<ScalarIntValue>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));
            structure.TestAddInstanceSuffix("BAR", new SetSuffix<ScalarIntValue>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

            Assert.AreEqual(TEST_VALUE, structure.GetSuffix("FOO"));
            structure.SetSuffix("FOO", TEST_VALUE - 10);
            Assert.AreEqual(TEST_VALUE - 10, structure.GetSuffix("FOO"));
            structure.SetSuffix("FOO", TEST_VALUE / 20);
            Assert.AreEqual(TEST_VALUE / 20, structure.GetSuffix("FOO"));
        }

        [Test]
        public void TwoSuffixesCanShareAModel()
        {
            const int TEST_VALUE = 12345;
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<ScalarIntValue>(TEST_VALUE);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<ScalarIntValue>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));
            structure.TestAddInstanceSuffix("BAR", new SetSuffix<ScalarIntValue>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

            Assert.AreEqual(TEST_VALUE, structure.GetSuffix("FOO"));
            Assert.AreEqual(TEST_VALUE, structure.GetSuffix("BAR"));
            structure.SetSuffix("FOO", TEST_VALUE - 10);
            Assert.AreEqual(TEST_VALUE - 10, structure.GetSuffix("FOO"));
            Assert.AreEqual(TEST_VALUE - 10, structure.GetSuffix("BAR"));
            structure.SetSuffix("BAR", TEST_VALUE / 20);
            Assert.AreEqual(TEST_VALUE / 20, structure.GetSuffix("BAR"));
            Assert.AreEqual(TEST_VALUE / 20, structure.GetSuffix("FOO"));
        }

        private static SuffixSetDlg<TParam> BuildBasicSetter<TParam>(StrongBox<TParam> state) where TParam : Encapsulation.Structure
        {
            SuffixSetDlg<TParam> setter = value => state.Value = value;
            return setter;
        }

        private static SuffixGetDlg<TParam>  BuildBasicGetter<TParam>(StrongBox<TParam> state) where TParam : Encapsulation.Structure
        {
            SuffixGetDlg<TParam> getter = () => state.Value;
            return getter;
        }
    }
}
