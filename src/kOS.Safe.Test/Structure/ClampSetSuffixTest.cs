using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class ClampSetSuffixTest
    {
        [Test]
        public void CanGet()
        {
            const int MIN_VALUE = 0;
            const int MAX_VALUE = 1;

            float value = 0.5f;
            var suffix = new ClampSetSuffix<ScalarDoubleValue>(() => value, i => value = i, MIN_VALUE, MAX_VALUE);

            Assert.AreEqual(new ScalarDoubleValue(value), suffix.Get().Value);

        }

        [Test]
        public void CanSet()
        {
            const int MIN_VALUE = 0;
            const int MAX_VALUE = 1;
            ScalarDoubleValue SET_VALUE = 0.5f;

            ScalarDoubleValue value = 0;
            var suffix = new ClampSetSuffix<ScalarDoubleValue>(() => value, i => value = i, MIN_VALUE, MAX_VALUE);

            suffix.Set(SET_VALUE);

            Assert.AreEqual(value, suffix.Get().Value);
            Assert.AreEqual(value, SET_VALUE);

        }

        [Test]
        public void CanSimpleClamp()
        {
            const int MIN_VALUE = 0;
            const int MAX_VALUE = 1;
            const float SET_VALUE = 1.5f;

            float value = 0;
            var suffix = new ClampSetSuffix<ScalarDoubleValue>(() => value, i => value = i, MIN_VALUE, MAX_VALUE);

            suffix.Set(SET_VALUE);

            Assert.AreEqual(value, suffix.Get());
            Assert.AreNotEqual(SET_VALUE, value);
            Assert.AreEqual(MAX_VALUE, value);

        }

        [Test]
        public void CanStepClamp()
        {
            const int MIN_VALUE = 0;
            const int MAX_VALUE = 1;
            const float SET_VALUE = 0.4f;
            const float EXPECTED_VALUE = 0.5f;
            const float STEP_VALUE = 0.5f;

            float value = 0;
            var suffix = new ClampSetSuffix<ScalarDoubleValue>(() => value, i => value = i, MIN_VALUE, MAX_VALUE, STEP_VALUE);

            suffix.Set(SET_VALUE);

            Assert.AreEqual(value, suffix.Get());
            Assert.AreNotEqual(SET_VALUE, value);
            Assert.AreEqual(EXPECTED_VALUE, value);
        }

        [Test]
        public void CanComplexStepClamp()
        {
            const int MIN_VALUE = 0;
            const int MAX_VALUE = 1;
            const float SET_VALUE = 1.4f;
            const float EXPECTED_VALUE = MAX_VALUE;
            const float STEP_VALUE = 0.5f;

            float value = 0;
            var suffix = new ClampSetSuffix<ScalarDoubleValue>(() => value, i => value = i, MIN_VALUE, MAX_VALUE, STEP_VALUE);

            suffix.Set(SET_VALUE);

            Assert.AreEqual(value, suffix.Get());
            Assert.AreNotEqual(SET_VALUE, value);
            Assert.AreEqual(EXPECTED_VALUE, value);
        }
    }
}
