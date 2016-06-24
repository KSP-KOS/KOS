using System;

namespace kOS.Safe.Test.Persistence
{
    public class HarddiskToArchiveCopyAndMoveTest : ArchiveAndHarddiskCopyAndMoveTest
    {
        public override kOS.Safe.Persistence.Volume SourceVolume {
            get {
                return harddisk;
            }
        }

        public override kOS.Safe.Persistence.Volume TargetVolume {
            get {
                return archive;
            }
        }
    }
}

