using System;
using NUnit.Framework;
using kOS.Safe.Persistence;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class HarddiskTest : VolumeTest
    {

        protected override int ExpectedCapacity {
            get {
                return 5000;
            }
        }

        protected override bool ExpectedRenameable {
            get {
                return true;
            }
        }

        [SetUp]
        public void Setup()
        {
            TestVolume = new Harddisk(ExpectedCapacity);
        }

    }
}

