using System;
using NUnit.Framework;
using kOS.Safe.Persistence;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class GlobalPathTest
    {
        [Test]
        public void CanReturnParent()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:/level1/level2");
            Assert.AreEqual("othervolume", path.VolumeId);
            Assert.AreEqual(2, path.Depth);
            Assert.AreEqual(2, path.Length);
        }

        [Test]
        public void CanIdentifyParents()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:/level1/level2");
            GlobalPath parent1 = GlobalPath.FromString("othervolume:");
            GlobalPath parent2 = GlobalPath.FromString("othervolume:/level1");
            GlobalPath notParent1 = GlobalPath.FromString("othervolume:/sthelse");
            GlobalPath notParent2 = GlobalPath.FromString("othervolume:/level1/level2/level3");
            GlobalPath notParent3 = GlobalPath.FromString("othervolume2:/level1/level2");

            Assert.IsTrue(parent1.IsParent(path));
            Assert.IsTrue(parent2.IsParent(path));
            Assert.IsFalse(path.IsParent(path));
            Assert.IsFalse(notParent1.IsParent(path));
            Assert.IsFalse(notParent2.IsParent(path));
            Assert.IsFalse(notParent3.IsParent(path));
        }

        [Test]
        public void CanHandleVolumeNames()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:/level1/level2");
            Assert.AreEqual("othervolume", path.VolumeId);
            Assert.AreEqual(2, path.Depth);
            Assert.AreEqual(2, path.Length);
        }

        [Test]
        public void CanHandleVolumeIds()
        {
            GlobalPath path = GlobalPath.FromString("1:level1/level2");
            Assert.AreEqual(1, path.VolumeId);
            Assert.AreEqual(2, path.Depth);
            Assert.AreEqual(2, path.Length);
        }

        [Test]
        public void CanHandleJustVolumeName()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:");
            Assert.AreEqual("othervolume", path.VolumeId);
            Assert.IsEmpty(path.Name);
            Assert.AreEqual(0, path.Depth);
            Assert.AreEqual(0, path.Length);
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanHandleGlobalPathWithLessThanZeroDepth()
        {
            GlobalPath.FromString("othervolume:/test/../../");
        }

        [Test]
        public void CanChangeName()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:123");
            GlobalPath newPath = path.ChangeName("abc");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(1, newPath.Length);
            Assert.AreEqual("abc", newPath.Name);

            path = GlobalPath.FromString("othervolume:/dir/file.jpg");
            newPath = path.ChangeName("new.txt");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(2, newPath.Length);
            Assert.AreEqual("new.txt", newPath.Name);
        }

        [Test]
        public void CanChangeExtension()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:123");
            GlobalPath newPath = path.ChangeExtension("txt");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(1, newPath.Length);
            Assert.AreEqual("123.txt", newPath.Name);

            path = GlobalPath.FromString("othervolume:/dir/file.jpg");
            newPath = path.ChangeExtension("txt");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(2, newPath.Length);
            Assert.AreEqual("file.txt", newPath.Name);

            path = GlobalPath.FromString("othervolume:/dir/complex.file..name..");
            newPath = path.ChangeExtension("txt");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(2, newPath.Length);
            Assert.AreEqual("complex.file..name..txt", newPath.Name);
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanHandleChangingExtensionOfRootPaths()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:");
            path.ChangeExtension("txt");
        }

        [Test]
        public void CanCombine()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:123");
            GlobalPath newPath = path.Combine("456", "789");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(3, newPath.Length);
            Assert.AreEqual("789", newPath.Name);

            newPath = path.Combine("..", "abc");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(1, newPath.Length);
            Assert.AreEqual("abc", newPath.Name);

            newPath = path.Combine("sub/abc");
            Assert.AreEqual("othervolume", newPath.VolumeId);
            Assert.AreEqual(3, newPath.Length);
            Assert.AreEqual("abc", newPath.Name);
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanFailToCombineOutside()
        {
            GlobalPath path = GlobalPath.FromString("othervolume:123");
            path.Combine("..", "..");
        }
    }
}