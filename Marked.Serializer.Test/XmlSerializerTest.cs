using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marked.Serializer.Test
{
    [TestClass]
    public class XmlSerializerTest
    {
        private XmlSerializer serializer;

        [TestInitialize]
        public void IntializeTest()
        {
            serializer = new XmlSerializer();
        }

        [TestMethod]
        public void TestPrimitiveSerializer()
        {
            var test = new PrimitiveTestObject
            {
                IntegerValue = 3,
                StringValue = "Test",
                SingleValue = 4.2f
            };
            
            var testString = serializer.SerializeToString(test);
            var newTestObject = serializer.Deserialize<PrimitiveTestObject>(testString);

            Assert.AreEqual(test.IntegerValue, newTestObject.IntegerValue);
            Assert.AreEqual(test.StringValue, newTestObject.StringValue);
            Assert.AreEqual(test.SingleValue, newTestObject.SingleValue, 0.0001f);
        }

        [TestMethod]
        public void TestSelfCycle()
        {
            var test = new DirectCycleTestObject
            {
                Value = 3
            };
            test.Child = test;
            
            var testString = serializer.SerializeToString(test);
            var newTestObject = serializer.Deserialize<DirectCycleTestObject>(testString);
            
            Assert.AreEqual(newTestObject, newTestObject.Child);
        }
    }
}