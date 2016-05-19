using System;
using NUnit.Framework;
using System.IO;
using kOS.Safe.Persistence;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class ArchiveToHarddiskCopyAndMoveTest : ArchiveAndHarddiskCopyAndMoveTest
    {
        public override kOS.Safe.Persistence.Volume SourceVolume {
            get {
                return archive;
            }
        }

        public override kOS.Safe.Persistence.Volume TargetVolume {
            get {
                return harddisk;
            }
        }
    }
}

