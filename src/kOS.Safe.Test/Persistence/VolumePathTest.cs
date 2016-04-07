using System;
using NUnit.Framework;
using kOS.Safe.Persistence;

namespace kOS.Safe.Test.Persistence
{
    [TestFixture]
    public class VolumePathTest
    {
        [Test]
        public void CanHandleEmptyPath()
        {
            VolumePath path = VolumePath.FromString("");
            Assert.AreEqual(0, path.Length);
            Assert.AreEqual(0, path.Depth);
            Assert.AreEqual(string.Empty, path.Name);
            Assert.AreEqual(string.Empty, path.Extension);
        }

        [Test]
        public void CanHandleRootPath()
        {
            VolumePath path = VolumePath.FromString("/");
            Assert.AreEqual(0, path.Length);
            Assert.AreEqual(0, path.Depth);
            Assert.AreEqual(string.Empty, path.Name);
            Assert.AreEqual(string.Empty, path.Extension);
        }

        [Test]
        public void CanHandleSimplePath()
        {
            VolumePath path = VolumePath.FromString("/identifier");
            Assert.AreEqual(1, path.Length);
            Assert.AreEqual(1, path.Depth);
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanHandleAbsolutePathWithParent()
        {
            VolumePath parent = VolumePath.FromString("/parent");
            VolumePath.FromString("/identifier", parent);
        }

        [Test]
        public void CanHandleTwoDots()
        {
            VolumePath parent = VolumePath.FromString("/parent/deeper/and_deeper");
            VolumePath path = VolumePath.FromString("../../", parent);
            Assert.AreEqual(1, path.Depth);
            Assert.AreEqual(1, path.Length);
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanHandlePathsThatPointOutside1()
        {
            VolumePath.FromString("/..");
        }

        [Test]
        [ExpectedException(typeof(KOSInvalidPathException))]
        public void CanHandlePathsThatPointOutside2()
        {
            VolumePath.FromString("/../test/test/test");
        }
    }
}

