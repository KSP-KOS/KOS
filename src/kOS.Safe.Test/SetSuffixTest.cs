using NUnit.Framework;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Test
{
    [TestFixture]
    public class SetSuffixTest
    {
        private class BasicClass<T>
        {
            public T State { get; set; }
        }

        [Test]
        public void CanGetDefaultValue()
        {
            var basicInstance = new BasicClass<int>();

            SuffixGetDlg<BasicClass<int>, int> getter = model => model.State;
            SuffixSetDlg<BasicClass<int>, int> setter = (model, value) => model.State = value;

            var suffix = new SetSuffix<BasicClass<int>, int>(basicInstance, getter, setter);

            Assert.IsNotNull(suffix);
            Assert.AreEqual(default(int), suffix.Get());
        }

        [Test]
        public void CanSetAndGet()
        {
            var basicInstance = new BasicClass<int>();

            SuffixGetDlg<BasicClass<int>, int> getter = model => model.State;
            SuffixSetDlg<BasicClass<int>, int> setter = (model, value) => model.State = value;

            var suffix = new SetSuffix<BasicClass<int>, int>(basicInstance, getter, setter);

            Assert.IsNotNull(suffix);
            suffix.Set(15);
            Assert.AreEqual(15,suffix.Get());

        }

        [Test]
        public void CanCoerceType()
        {
            var basicInstance = new BasicClass<int>();

            SuffixGetDlg<BasicClass<int>, int> getter = model => model.State;
            SuffixSetDlg<BasicClass<int>, int> setter = (model, value) => model.State = value;

            var suffix = new SetSuffix<BasicClass<int>, int>(basicInstance, getter, setter);

            const double TEST_VALUE = 15.0d;
            Assert.IsNotNull(suffix);
            suffix.Set(TEST_VALUE);
            var finalValue = suffix.Get();
            Assert.AreEqual(TEST_VALUE,finalValue);

        }

        [Test]
        public void CanCoerceAndTruncateType()
        {
            var basicInstance = new BasicClass<int>();

            SuffixGetDlg<BasicClass<int>, int> getter = model => model.State;
            SuffixSetDlg<BasicClass<int>, int> setter = (model, value) => model.State = value;

            var suffix = new SetSuffix<BasicClass<int>, int>(basicInstance, getter, setter);

            const double TEST_VALUE = 15.1234d;
            const double TEST_VALUE_TRUNCATED = 15.0d;
            Assert.IsNotNull(suffix);
            suffix.Set(TEST_VALUE);
            var finalValue = suffix.Get();
            Assert.AreEqual(TEST_VALUE_TRUNCATED,finalValue);

        }
    }
}
