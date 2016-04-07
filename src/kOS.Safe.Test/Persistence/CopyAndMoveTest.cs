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
        protected string file1 = "file1", file2 = "file2", file3 = "file3";

        protected GlobalPath dir1Path, subdir1Path, subdir2Path, subsubdir1Path;

        protected GlobalPath file1Path, dir1File1Path, dir1File2Path, subdir1File1Path, subsubdir1File1Path;

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
            subdir1File1Path = subdir1Path.Combine(file1);
            subsubdir1File1Path = subsubdir1Path.Combine(file1);

            SourceVolume.Clear();
            TargetVolume.Clear();

            SourceVolume.CreateDirectory(subdir2Path);
            SourceVolume.CreateDirectory(subsubdir1Path);

            SourceVolume.CreateFile(file1Path).WriteLn(file1);
            SourceVolume.CreateFile(subsubdir1File1Path).WriteLn("subsubdir1File1");

            var dir1List = SourceVolume.Root.List();

            GlobalPath targetPath = GlobalPath.FromString("1:/dir1/file1");

        }

        [Test]
        public void CanCopyFileToExistingFile()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1/file1");
            TargetVolume.CreateFile(targetPath);
            volumeManager.Copy(subsubdir1File1Path, targetPath);

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }

        [Test]
        public void CanCopyFileToNewFile()
        {
            volumeManager.Copy(subsubdir1File1Path, GlobalPath.FromString("1:/dir1/file1"));

            Assert.AreEqual(1, TargetVolume.Root.List().Count);
            VolumeDirectory parent = (TargetVolume.Open(dir1Path) as VolumeDirectory);
            Assert.AreEqual(1, parent.List().Count);
            Assert.AreEqual("subsubdir1File1\n", (parent.List()[file1] as VolumeFile).ReadAll().String);
        }


        [Test]
        public void CanCopyFileToDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/dir1");
            TargetVolume.CreateDirectory(targetPath);
            volumeManager.Copy(subsubdir1File1Path, targetPath);

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
            volumeManager.Copy(dir1Path, GlobalPath.FromString("1:/newdirectory"));

            CompareDirectories(dir1Path, GlobalPath.FromString("1:/newdirectory/" + dir1));
        }

        [Test]
        public void CanCopyDirectoryToNewDirectory()
        {
            GlobalPath targetPath = GlobalPath.FromString("1:/newname");
            volumeManager.Copy(dir1Path, targetPath);

            CompareDirectories(dir1Path, targetPath);
        }

        /*
        [Test]
        public void CanFailIfThereIsNoSpaceToCopy()
        {
            Assert.Fail();
        }

        [Test]
        public void CanMoveEvenIfThereIsNoSpaceToCopy()
        {
            Assert.Fail();
        }
        */

        private void CompareDirectories(GlobalPath dir1Path, GlobalPath dir2Path)
        {
            Volume dir1Volume = volumeManager.GetVolumeFromPath(dir1Path);
            Volume dir2Volume = volumeManager.GetVolumeFromPath(dir2Path);

            VolumeDirectory dir1 = dir1Volume.Open(dir1Path) as VolumeDirectory;
            VolumeDirectory dir2 = dir2Volume.Open(dir2Path) as VolumeDirectory;

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

