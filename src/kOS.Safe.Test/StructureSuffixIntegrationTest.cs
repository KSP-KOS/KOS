using System.Runtime.CompilerServices;
using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class StructureSuffixIntegrationTest
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            Debug.Logger = new TestLogger();
        }

        [Test]
        public void CanAddSuffix()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<bool>(false);
            structure.TestAddInstanceSuffix("FOO", new Suffix<bool>(BuildBasicGetter(strongBox)));
            Assert.IsFalse((bool) structure.GetSuffix("FOO"));
        }

        [Test]
        public void CanReflectChangeInUnderlyingData()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<bool>(false);
            structure.TestAddInstanceSuffix("FOO", new Suffix<bool>(BuildBasicGetter(strongBox)));

            strongBox.Value = false;
            Assert.IsFalse((bool) structure.GetSuffix("FOO"));
            strongBox.Value = true;
            Assert.IsTrue((bool) structure.GetSuffix("FOO"));
        }

        [Test]
        public void CanAddSetSuffix()
        {
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<bool>(false);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<bool>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

            Assert.IsFalse((bool) structure.GetSuffix("FOO"));
        }

        [Test]
        public void CanSetSuffix()
        {
            const int TEST_VALUE = 12345;
            var structure = new StructureTest.TestStructure();
            var strongBox = new StrongBox<int>(TEST_VALUE);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<int>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));
            structure.TestAddInstanceSuffix("BAR", new SetSuffix<int>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

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
            var strongBox = new StrongBox<int>(TEST_VALUE);
            structure.TestAddInstanceSuffix("FOO", new SetSuffix<int>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));
            structure.TestAddInstanceSuffix("BAR", new SetSuffix<int>(BuildBasicGetter(strongBox), BuildBasicSetter(strongBox)));

            Assert.AreEqual(TEST_VALUE, structure.GetSuffix("FOO"));
            Assert.AreEqual(TEST_VALUE, structure.GetSuffix("BAR"));
            structure.SetSuffix("FOO", TEST_VALUE - 10);
            Assert.AreEqual(TEST_VALUE - 10, structure.GetSuffix("FOO"));
            Assert.AreEqual(TEST_VALUE - 10, structure.GetSuffix("BAR"));
            structure.SetSuffix("BAR", TEST_VALUE / 20);
            Assert.AreEqual(TEST_VALUE / 20, structure.GetSuffix("BAR"));
            Assert.AreEqual(TEST_VALUE / 20, structure.GetSuffix("FOO"));
        }

        private static SuffixSetDlg<TParam> BuildBasicSetter<TParam>(StrongBox<TParam> state)
        {
            SuffixSetDlg<TParam> setter = value => state.Value = value;
            return setter;
        }

        private static SuffixGetDlg<TParam>  BuildBasicGetter<TParam>(StrongBox<TParam> state)
        {
            SuffixGetDlg<TParam> getter = () => state.Value;
            return getter;
        }
    }
}
