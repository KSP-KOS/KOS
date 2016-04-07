using System;
using NUnit.Framework;
using System.IO;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Safe.Exceptions;
using System.Text;

namespace kOS.Safe.Test
{
    public abstract class VolumeTest
    {
        public Volume TestVolume { get; set; }
        protected abstract int ExpectedCapacity { get; }
        protected abstract bool ExpectedRenameable { get; }

        [SetUp]
        public void SetupLogger()
        {
            SafeHouse.Logger = new TestLogger();
        }


        [Test]
        public void CanReturnCapacity()
        {
            Assert.AreEqual(ExpectedCapacity, TestVolume.Capacity);
        }

        [Test]
        public void CanReturnRenameable()
        {
            Assert.AreEqual(ExpectedRenameable, TestVolume.Renameable);
        }

        [Test]
        public void CanCreateDirectories()
        {
            string dir1 = "/testdir", dir2 = "/abc", dir3 = "/abc2", dir4 = "/abc/subdirectory";
            Assert.AreEqual(0, TestVolume.Root.List().Count);

            TestVolume.CreateDirectory(VolumePath.FromString(dir1));
            TestVolume.CreateDirectory(VolumePath.FromString(dir2));
            TestVolume.CreateDirectory(VolumePath.FromString(dir3));
            TestVolume.CreateDirectory(VolumePath.FromString(dir4));

            Assert.AreEqual(3, TestVolume.Root.List().Count);
            Assert.AreEqual(dir2, TestVolume.Root.List()["abc"].Path.ToString());
            Assert.AreEqual(dir3, TestVolume.Root.List()["abc2"].Path.ToString());
            Assert.AreEqual(dir1, TestVolume.Root.List()["testdir"].Path.ToString());

            Assert.AreEqual(1, (TestVolume.Root.List()["abc"] as VolumeDirectory).List().Values.Count);
            Assert.AreEqual(dir4, (TestVolume.Root.List()["abc"] as VolumeDirectory).List()["subdirectory"].Path.ToString());
        }

        [Test]
        public void CanCreateSubdirectories()
        {
            string parent1 = "/parent1", parent2 = "/parent2";
            string dir1 = parent1 + "/sub1", dir2 = parent1 + "/sub2", dir3 = parent2 + "/sub3";
            Assert.AreEqual(0, TestVolume.Root.List().Count);

            TestVolume.CreateDirectory(VolumePath.FromString(dir1));
            TestVolume.CreateDirectory(VolumePath.FromString(dir2));
            TestVolume.CreateDirectory(VolumePath.FromString(dir3));

            Assert.AreEqual(2, TestVolume.Root.List().Count);
            Assert.AreEqual(parent1, TestVolume.Root.List()["parent1"].Path.ToString());
            Assert.AreEqual(parent2, TestVolume.Root.List()["parent2"].Path.ToString());

            VolumeDirectory dir = TestVolume.Open(VolumePath.FromString(parent1)) as VolumeDirectory;
            Assert.AreEqual(2, dir.List().Count);
            Assert.AreEqual(dir1, dir.List()["sub1"].Path.ToString());
            Assert.AreEqual(dir2, dir.List()["sub2"].Path.ToString());
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenCreatingDirectoryOverExistingDirectory()
        {
            string parent = "/parent1";
            string dir = parent + "/sub1";

            TestVolume.CreateDirectory(VolumePath.FromString(dir));
            TestVolume.CreateDirectory(VolumePath.FromString(dir));
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenCreatingDirectoryOverFile()
        {
            string parent1 = "/parent1";
            string file1 = parent1 + "/sub1";

            TestVolume.CreateFile(VolumePath.FromString(file1));
            TestVolume.CreateDirectory(VolumePath.FromString(file1));
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanFailWhenCreatingDirectoryWithNegativeDepth()
        {
            string dir = "/../test";

            TestVolume.CreateDirectory(VolumePath.FromString(dir));
        }

        [Test]
        public void CanDeleteDirectories()
        {
            string parent1 = "/parent1", parent2 = "/parent2";
            string dir1 = parent1 + "/sub1", dir2 = parent1 + "/sub2", dir3 = parent2 + "/sub3";

            TestVolume.CreateDirectory(VolumePath.FromString(dir1));
            TestVolume.CreateDirectory(VolumePath.FromString(dir2));
            TestVolume.CreateDirectory(VolumePath.FromString(dir3));

            VolumeDirectory parent = TestVolume.Open(VolumePath.FromString(parent1)) as VolumeDirectory;

            TestVolume.Delete(VolumePath.FromString(dir1));
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual(dir2, parent.List()["sub2"].Path.ToString());

            TestVolume.Delete(VolumePath.FromString(parent2));
            Assert.AreEqual(1, TestVolume.Root.List().Count);
            Assert.AreEqual(parent1, TestVolume.Root.List()["parent1"].Path.ToString());
        }

        [Test]
        public void CanDeleteNonExistingDirectories()
        {
            VolumePath path = VolumePath.FromString("/abc");
            TestVolume.CreateDirectory(path);
            TestVolume.Delete(path);
            TestVolume.Delete(path);
        }

        [Test]
        public void CanCreateFiles()
        {
            string parent1 = "/parent1", parent2 = "/parent2";
            string file1 = parent1 + "/sub1", file2 = parent1 + "/sub2", file3 = parent2 + "/sub3";

            TestVolume.CreateFile(VolumePath.FromString(file1));
            TestVolume.CreateFile(VolumePath.FromString(file2));
            TestVolume.CreateFile(VolumePath.FromString(file3));

            Assert.AreEqual(2, TestVolume.Root.List().Count);
            Assert.AreEqual(parent1, TestVolume.Root.List()["parent1"].Path.ToString());
            Assert.AreEqual(parent2, TestVolume.Root.List()["parent2"].Path.ToString());

            VolumeDirectory dir = TestVolume.Open(VolumePath.FromString(parent1)) as VolumeDirectory;
            Assert.AreEqual(2, dir.List().Count);
            Assert.AreEqual(file1, dir.List()["sub1"].Path.ToString());
            Assert.IsInstanceOf<VolumeFile>(dir.List()["sub1"]);
            Assert.AreEqual(file2, dir.List()["sub2"].Path.ToString());
            Assert.IsInstanceOf<VolumeFile>(dir.List()["sub2"]);

            dir = TestVolume.Open(VolumePath.FromString(parent2)) as VolumeDirectory;
            Assert.AreEqual(1, dir.List().Count);
            Assert.AreEqual(file3, dir.List()["sub3"].Path.ToString());
            Assert.IsInstanceOf<VolumeFile>(dir.List()["sub3"]);
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenCreatingFileOverExistingFile()
        {
            string parent = "/parent1";
            string file = parent + "/file";

            TestVolume.CreateFile(VolumePath.FromString(file));
            TestVolume.CreateFile(VolumePath.FromString(file));
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenCreatingFileOverDirectory()
        {
            string parent1 = "/parent1";
            string file1 = parent1 + "/sub1";

            TestVolume.CreateDirectory(VolumePath.FromString(file1));
            TestVolume.CreateFile(VolumePath.FromString(file1));
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanFailWhenCreatingFileWithNegativeDepth()
        {
            string dir = "/../test";

            TestVolume.CreateFile(VolumePath.FromString(dir));
        }

        [Test]
        public void CanReadAndWriteFiles()
        {
            string dir = "/content_parent/content_test";
            string content = "some test content!@#$;\n\rtaenstałąż";
            int contentLength = Encoding.UTF8.GetBytes(content).Length;

            VolumeFile volumeFile = TestVolume.CreateFile(VolumePath.FromString(dir));

            Assert.AreEqual(0, volumeFile.ReadAll().Bytes.Length);
            Assert.AreEqual("", volumeFile.ReadAll().String);

            Assert.IsTrue(volumeFile.Write(content));
            Assert.AreEqual(FileCategory.ASCII, volumeFile.ReadAll().Category);

            Assert.AreEqual(contentLength, TestVolume.Size);

            if (ExpectedCapacity != Volume.INFINITE_CAPACITY)
            {
                Assert.AreEqual(ExpectedCapacity - contentLength, TestVolume.FreeSpace);
            } else
            {
                Assert.AreEqual(Volume.INFINITE_CAPACITY, TestVolume.FreeSpace);
            }

            Assert.AreEqual(contentLength, volumeFile.Size);
            Assert.AreEqual(content, volumeFile.ReadAll().String);

            // we should be able to save the same file again
            Assert.IsTrue(TestVolume.Save(volumeFile) != null);
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenSavingFileOverDirectory()
        {
            string parent1 = "/parent1";
            string file1 = parent1 + "/sub1";

            TestVolume.CreateDirectory(VolumePath.FromString(file1));
            TestVolume.Save(VolumePath.FromString(file1), new FileContent());
        }

        [Test]
        public void CanHandleOpeningNonExistingFiles()
        {
            Assert.IsNull(TestVolume.Open(VolumePath.FromString("/idonotexist")));
        }

        [Test]
        public void CanDeleteFiles()
        {
            string parent1 = "/parent1";
            string file1 = parent1 + "/sub1", file2 = parent1 + "/sub2";

            TestVolume.CreateFile(VolumePath.FromString(file1));
            TestVolume.CreateFile(VolumePath.FromString(file2));

            Assert.IsTrue(TestVolume.Delete(VolumePath.FromString(file1)));

            VolumeDirectory dir = TestVolume.Open(VolumePath.FromString(parent1)) as VolumeDirectory;
            Assert.AreEqual(1, dir.List().Count);
            Assert.AreEqual(file2, dir.List()["sub2"].Path.ToString());
            Assert.IsInstanceOf<VolumeFile>(dir.List()["sub2"]);
        }

        [Test]
        public void CanDeleteNonExistingFiles()
        {
            VolumePath path = VolumePath.FromString("/abc");
            TestVolume.CreateFile(path);

            // Delete the file twice
            TestVolume.Delete(path);
            TestVolume.Delete(path);
        }

    }
}

