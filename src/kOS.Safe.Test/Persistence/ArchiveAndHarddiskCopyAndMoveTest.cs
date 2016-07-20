using System;
using NUnit.Framework;
using System.IO;
using kOS.Safe.Persistence;

namespace kOS.Safe.Test.Persistence
{
    public abstract class ArchiveAndHarddiskCopyAndMoveTest : CopyAndMoveTest
    {
        public string KosTestDirectory = "kos_archive_tests";
        protected string archivePath;
        protected Archive archive;
        protected Harddisk harddisk;

        public ArchiveAndHarddiskCopyAndMoveTest()
        {
            archivePath = Path.Combine(Path.GetTempPath(), KosTestDirectory);

            archive = PrepareArchive(archivePath);
            harddisk = new Harddisk(1000);
        }

        private Archive PrepareArchive(string archivePath)
        {
            if (Directory.Exists(archivePath))
            {
                Directory.Delete(archivePath, true);
            }

            Directory.CreateDirectory(archivePath);

            return new Archive(archivePath);
        }
    }
}

