using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Marked.Serializer.Test
{
    [TestClass]
    public class CycleUtilityTest
    {
        [TestMethod]
        public void ValidCycleTypeInt()
        {
            Assert.IsFalse(CycleUtility.ValidCycleType(typeof(int)));
        }

        [TestMethod]
        public void ValidCycleTypeGuid()
        {
            Assert.IsFalse(CycleUtility.ValidCycleType(typeof(Guid)));
        }

        [TestMethod]
        public void ValidCycleTypeEnum()
        {
            Assert.IsFalse(CycleUtility.ValidCycleType(typeof(MidpointRounding)));
        }

        [TestMethod]
        public void ValidCycleTypeString()
        {
            Assert.IsTrue(CycleUtility.ValidCycleType(typeof(string)));
        }

        [TestMethod]
        public void ValidCycleTypeObject()
        {
            Assert.IsTrue(CycleUtility.ValidCycleType(typeof(Object)));
        }
    }
}