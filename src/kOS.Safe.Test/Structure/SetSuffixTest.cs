using System.Runtime.CompilerServices;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class SetSuffixTest
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            SafeHouse.Logger = new TestLogger();
        }

        // Deleted the CanGetDefaultValue test because all structures
        // are now reference types with a default value of null.

        [Test]
        public void CanSetAndGet()
        {
            var suffix = BuildBasicSetSuffix<ScalarIntValue>();

            Assert.IsNotNull(suffix);
            suffix.Set(15);
            Assert.AreEqual(ScalarValue.Create(15),suffix.Get().Value);
        }

        private static SetSuffix<TParam> BuildBasicSetSuffix<TParam>() where TParam : Encapsulation.Structure
        {
            var basicInstance = new StrongBox<TParam>(default(TParam));

            SuffixSetDlg<TParam> setter = value => basicInstance.Value = value;
            SuffixGetDlg<TParam> getter = () => basicInstance.Value;

            return new SetSuffix<TParam>(getter, setter);
        }

        [Test]
        public void CanCoerceType()
        {
            var suffix = BuildBasicSetSuffix<ScalarIntValue>();

            const double TEST_VALUE = 15.0d;
            Assert.IsNotNull(suffix);
            suffix.Set(TEST_VALUE);
            var finalValue = suffix.Get().Value;
            Assert.AreEqual(ScalarValue.Create(TEST_VALUE), finalValue);
        }

        [Test]
        public void CanCoerceAndTruncateType()
        {
            var suffix = BuildBasicSetSuffix<ScalarIntValue>();

            const double TEST_VALUE = 15.1234d;
            const double TEST_VALUE_TRUNCATED = 15;
            Assert.IsNotNull(suffix);
            suffix.Set(TEST_VALUE);
            var finalValue = suffix.Get().Value;
            Assert.AreEqual(ScalarValue.Create(TEST_VALUE_TRUNCATED), finalValue);
        }

        [Test]
        public void CanCoerceAndExtendType()
        {
            var suffix = BuildBasicSetSuffix<ScalarIntValue>();

            const int TEST_VALUE = 15;
            Assert.IsNotNull(suffix);
            suffix.Set(TEST_VALUE);
            var finalValue = suffix.Get().Value;
            Assert.AreEqual(ScalarValue.Create(TEST_VALUE), finalValue);
        }
    }
}
