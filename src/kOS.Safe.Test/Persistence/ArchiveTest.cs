using System;
using NUnit.Framework;
using System.IO;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class ArchiveTest : VolumeTest
    {
        public const string KosTestDirectory = "kos_archive_tests";

        protected override int ExpectedCapacity {
            get {
                return Volume.INFINITE_CAPACITY;
            }
        }

        protected override bool ExpectedRenameable {
            get {
                return false;
            }
        }

        protected string testPath = Path.Combine(Path.GetTempPath(), KosTestDirectory);

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }

            Directory.CreateDirectory(testPath);

            TestVolume = new Archive(testPath);
        }
    }
}

