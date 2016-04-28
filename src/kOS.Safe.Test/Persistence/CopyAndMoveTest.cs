using System;
using NUnit.Framework;
using System.IO;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Safe.Exceptions;
using System.Text;
using System.Collections.Generic;

namespace kOS.Safe.Test
{
    public abstract class CopyAndMoveTest
    {
        public abstract Volume SourceVolume { get; }
        public abstract Volume TargetVolume { get; }

        protected VolumeManager volumeManager;

        protected string dir1 = "dir1", subdir1 = "subdir1", subdir2 = "subdir2", subsubdir1 = "subsubdir1";
        protected string file1 = "file1", file2 = "file2", file3 = "file3.ks";

        protected GlobalPath dir1Path, subdir1Path, subdir2Path, subsubdir1Path;

        protected GlobalPath file1Path, dir1File1Path, dir1File2Path, dir1File3Path, subdir1File1Path, subsubdir1File1Path;

        [SetUp]
        public void SetupLogger()
        {
            SafeHouse.Logger = new TestLogger();
        }

        [SetUp]
        public void SetupVolumeManager()
        {
            volumeManager = new VolumeManager();
            volumeManager.Add(SourceVolume);
            volumeManager.Add(TargetVolume);
        }

        [SetUp]
        public void SetupVolumes()
        {
            dir1Path = GlobalPath.FromString("0:" + dir1);
            subdir1Path = dir1Path.Combine(subdir1);
            subdir2Path = dir1Path.Combine(subdir2);
            subsubdir1Path = subdir1Path.Combine(subsubdir1);

            file1Path = GlobalPath.FromString("0:" + file1);
            dir1File1Path = dir1Path.Combine(file1);
            dir1File2Path = dir1Path.Combine(file2);
            dir1File3Path = dir1Path.Combine(file3);
            subdir1File1Path = subdir1Path.Combine(file1);
            subsubdir1File1Path = subsubdir1Path.Combine(file1);

            SourceVolume.Clear();
            TargetVolume.Clear();

            SourceVolume.CreateDirectory(subdir2Path);
            SourceVolume.CreateDirectory(subsubdir1Path);

            SourceVolume.CreateFile(file1Path).WriteLn(file1);
            SourceVolume.CreateFile(dir1File3Path).WriteLn(file2);
            SourceVolume.CreateFile(subsubdir1File1Path).WriteLn("subsubdir1File1");
        }

        [Test]
        public void CanCopyFileToExistingFile()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1/file1");
            TargetVolume.CreateFile(targetPath);
            Assert.IsTrue(volumeManager.Copy(subsubdir1File1Path, targetPath));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }

        [Test]
        public void CanCopyFileToNewFile()
        {
            Assert.IsTrue(volumeManager.Copy(subsubdir1File1Path, GlobalPath.FromString("1:/dir1/file1")));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }

        [Test]
        public void CanCopyFileToRootDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:");
            Assert.IsTrue(volumeManager.Copy(subsubdir1File1Path, targetPath));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeFile file = (TargetVolume.Open(file1) as VolumeFile);
            Assert.AreEqual("subsubdir1File1\n", file.ReadAll().String);
        }

        [Test]
        public void CanCopyFileByCookedName()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:");
            Assert.IsTrue(volumeManager.Copy(dir1Path.Combine("file3"), targetPath));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            Assert.IsTrue(TargetVolume.Root.List()[file3] is VolumeFile);
        }

        [Test]
        public void CanCopyFileToSubdirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1");
            TargetVolume.CreateDirectory(targetPath);
            Assert.IsTrue(volumeManager.Copy(subsubdir1File1Path, targetPath));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenTryingToCopyDirectoryToAFile()
        {
            VolumePath filePath = TargetVolume.CreateFile("newfile").Path;
            volumeManager.Copy(dir1Path, GlobalPath.FromString("1:/newfile"));
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenTryingToCopyDirectoryIntoItself()
        {
            volumeManager.Copy(dir1Path, subdir1Path);
        }

        [Test]
        public void CanCopyDirectoryToExistingDirectory()
        {
            TargetVolume.CreateDirectory(VolumePath.FromString("/newdirectory"));
            Assert.IsTrue(volumeManager.Copy(dir1Path, GlobalPath.FromString("1:/newdirectory")));

            CompareDirectories(dir1Path, GlobalPath.FromString("1:/newdirectory/" + dir1));
        }

        [Test]
        public void CanCopyDirectoryToNewDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/newname");
            Assert.IsTrue(volumeManager.Copy(dir1Path, targetPath));

            CompareDirectories(dir1Path, targetPath);
        }

        [Test]
        public void CanCopyDirectoryToRootDirectory()
        {
            Assert.IsTrue(volumeManager.Copy(dir1Path, GlobalPath.FromString("1:/")));

            CompareDirectories(dir1Path, GlobalPath.FromString("1:/" + dir1));
        }

        [Test]
        public void CanCopyRootDirectoryToRootDirectory()
        {
            Assert.IsTrue(volumeManager.Copy(GlobalPath.FromString("0:/"), GlobalPath.FromString("1:/")));

            CompareDirectories(GlobalPath.FromString("0:/"), GlobalPath.FromString("1:/"));
        }

        [Test]
        public void CanCopyRootDirectoryToExistingDirectory()
        {
            TargetVolume.CreateDirectory(VolumePath.FromString("/newdirectory"));
            Assert.IsTrue(volumeManager.Copy(GlobalPath.FromString("0:/"), GlobalPath.FromString("1:/newdirectory")));

            CompareDirectories(GlobalPath.FromString("0:/"), GlobalPath.FromString("1:/newdirectory"));
        }

        [Test]
        public void CanCopyRootDirectoryToRootDirectoryTwice()
        {
            GlobalPath source = GlobalPath.FromString("0:/");
            GlobalPath destination = GlobalPath.FromString("1:/");
            Assert.IsTrue(volumeManager.Copy(source, destination));
            Assert.IsTrue(volumeManager.Copy(source, destination));

            CompareDirectories(source, destination);
        }

        [Test]
        public void CanFailToCopyFileIfThereIsNoSpaceToCopy()
        {
            if (TargetVolume.Capacity == Volume.INFINITE_CAPACITY)
            {
                Assert.Pass();
                return;
            }

            (SourceVolume.Open(subsubdir1File1Path) as VolumeFile)
                .WriteLn(new string('a', (int)TargetVolume.Capacity / 2 + 1));
            Assert.IsTrue(volumeManager.Copy(subsubdir1File1Path, GlobalPath.FromString("1:/copy1")));
            Assert.IsFalse(volumeManager.Copy(subsubdir1File1Path, GlobalPath.FromString("1:/copy2")));
        }

        [Test]
        public void CanFailToCopyDirectoryIfThereIsNoSpaceToCopy()
        {
            if (TargetVolume.Capacity == Volume.INFINITE_CAPACITY)
            {
                Assert.Pass();
                return;
            }

            (SourceVolume.Open(subsubdir1File1Path) as VolumeFile)
                .WriteLn(new string('a', (int)TargetVolume.Capacity / 4 + 1));
            SourceVolume.CreateFile(subdir1Path.Combine("other"))
                .WriteLn(new string('a', (int)TargetVolume.Capacity / 4 + 1));
            Assert.IsTrue(volumeManager.Copy(subdir1Path, GlobalPath.FromString("1:/copy1")));
            Assert.IsFalse(volumeManager.Copy(subdir1Path, GlobalPath.FromString("1:/copy2")));
        }


        [Test]
        public void CanMoveFileToExistingFile()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1/file1");
            TargetVolume.CreateFile(targetPath);
            Assert.IsTrue(volumeManager.Move(subsubdir1File1Path, targetPath));

            Assert.IsFalse(SourceVolume.Exists(subsubdir1File1Path));
            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);

        }

        [Test]
        public void CanMoveFileToNewFile()
        {
            Assert.IsTrue(volumeManager.Move(subsubdir1File1Path, GlobalPath.FromString("1:/dir1/file1")));

            Assert.IsFalse(SourceVolume.Exists(subsubdir1File1Path));
            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }


        [Test]
        public void CanMoveFileToDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1");
            TargetVolume.CreateDirectory(targetPath);
            Assert.IsTrue(volumeManager.Move(subsubdir1File1Path, targetPath));

            Assert.IsFalse(SourceVolume.Exists(subsubdir1File1Path));
            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);

        }

        [Test]
        public void CanMoveFileByCookedName()
        {
            var sourcePath = dir1Path.Combine("file3");
            GlobalPath targetPath = GlobalPath.FromString("1:");
            Assert.IsTrue(volumeManager.Move(sourcePath, targetPath));

            Assert.IsFalse(SourceVolume.Exists(sourcePath));
            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            Assert.IsTrue(TargetVolume.Root.List()[file3] is VolumeFile);
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenTryingToMoveDirectoryToAFile()
        {
            VolumePath filePath = TargetVolume.CreateFile("newfile").Path;
            volumeManager.Move(dir1Path, GlobalPath.FromString("1:/newfile"));
        }

        [Test]
        [ExpectedException(typeof(KOSPersistenceException))]
        public void CanFailWhenTryingToMoveDirectoryIntoItself()
        {
            volumeManager.Move(dir1Path, subdir1Path);
        }

        [Test]
        public void CanMoveDirectoryToExistingDirectory()
        {
            TargetVolume.CreateDirectory(VolumePath.FromString("/newdirectory"));
            Assert.IsTrue(volumeManager.Move(dir1Path, GlobalPath.FromString("1:/newdirectory")));
            Assert.IsFalse(SourceVolume.Exists(dir1Path));
        }

        [Test]
        public void CanMoveDirectoryToNewDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/newname");
            Assert.IsTrue(volumeManager.Move(dir1Path, targetPath));
            Assert.IsFalse(SourceVolume.Exists(dir1Path));
        }

        [Test]
        public void CanFailToMoveWhenTheresNoSpaceOnTargetVolume()
        {
            if (TargetVolume.Capacity == Volume.INFINITE_CAPACITY)
            {
                Assert.Pass();
                return;
            }

            (SourceVolume.Open(subsubdir1File1Path) as VolumeFile)
                .WriteLn(new string('a', (int)TargetVolume.Capacity / 2 + 1));
            Assert.IsTrue(volumeManager.Copy(subdir1Path, GlobalPath.FromString("1:/copy1")));
            Assert.IsFalse(volumeManager.Move(subdir1Path, GlobalPath.FromString("1:/copy2")));
        }

        [Test]
        public void CanMoveEvenIfThereIsNoSpaceOnSameVolume()
        {
            if (SourceVolume.Capacity == Volume.INFINITE_CAPACITY)
            {
                Assert.Pass();
                return;
            }

            (SourceVolume.Open(subsubdir1File1Path) as VolumeFile)
                .WriteLn(new string('a', (int)SourceVolume.Capacity / 2 + 1));
            Assert.IsTrue(volumeManager.Move(subdir1Path, GlobalPath.FromString("0:/newname")));
        }

        private void CompareDirectories(GlobalPath dir1Path, GlobalPath dir2Path)
        {
            Volume dir1Volume = volumeManager.GetVolumeFromPath(dir1Path);
            Volume dir2Volume = volumeManager.GetVolumeFromPath(dir2Path);

            VolumeDirectory dir1 = dir1Volume.Open(dir1Path) as VolumeDirectory;
            VolumeDirectory dir2 = dir2Volume.Open(dir2Path) as VolumeDirectory;

            Assert.NotNull(dir1);
            Assert.NotNull(dir2);

            int dir1Count = dir1.List().Count;
            int dir2Count = dir2.List().Count;

            if (dir1Count != dir2Count)
            {
                Assert.Fail("Item count not equal: " + dir1Count + " != " + dir2Count);
            }

            foreach (KeyValuePair<string, VolumeItem> pair in dir1.List())
            {
                VolumeItem dir2Item = dir2Volume.Open(dir2Path.Combine(pair.Key));

                if (pair.Value is VolumeDirectory && dir2Item is VolumeDirectory)
                {
                    CompareDirectories(dir1Path.Combine(pair.Key), dir2Path.Combine(pair.Key));
                } else if (pair.Value is VolumeFile && dir2Item is VolumeFile)
                {
                    VolumeFile file1 = pair.Value as VolumeFile;
                    VolumeFile file2 = dir2Item as VolumeFile;

                    Assert.AreEqual(file1.ReadAll(), file2.ReadAll());
                } else
                {
                    Assert.Fail("Items are not of the same type: " + dir1Path.Combine(pair.Key) + ", " + dir2Path.Combine(pair.Key));
                }
            }
        }
    }
}

