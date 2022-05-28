using System;
using NUnit.Framework;
using kOS.Safe.Persistence;
using kOS.Safe.Exceptions;

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
        public void CanHandleMultipleSlashes()
        {
            VolumePath path = VolumePath.FromString("//test//test2/");
            Assert.AreEqual(2, path.Length);
            Assert.AreEqual(2, path.Depth);
            Assert.AreEqual("test2", path.Name);
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

        [Test]
        public void CanCombinePaths()
        {
            VolumePath path = VolumePath.FromString("/test");
            VolumePath combined = path.Combine("sub1", "sub2");
            VolumePath combined2 = path.Combine("..");

            Assert.AreEqual(3, combined.Depth);
            Assert.AreEqual(0, combined2.Depth);
        }

        [Test]
        public void CanCombinePathsThatContainSlashes()
        {
            VolumePath path = VolumePath.FromString("/test");
            VolumePath combined = path.Combine("sub1/sub2");
            VolumePath combined2 = path.Combine("sub1/..");

            Assert.AreEqual(3, combined.Depth);
            Assert.AreEqual(1, combined2.Depth);
        }
    }
}

