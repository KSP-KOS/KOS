using System;
using kOS.Safe.Persistence;
using NUnit.Framework;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class HarddiskToHarddiskCopyAndMoveTest : CopyAndMoveTest
    {
        protected Harddisk harddisk1, harddisk2;

        public HarddiskToHarddiskCopyAndMoveTest()
        {
            harddisk1 = new Harddisk(1000);
            harddisk2 = new Harddisk(1000);
        }

        public override Volume SourceVolume {
            get {
                return harddisk1;
            }
        }
        public override Volume TargetVolume {
            get {
                return harddisk2;
            }
        }
    }
}

