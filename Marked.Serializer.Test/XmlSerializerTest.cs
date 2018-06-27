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
        public void TestComplexSerializer()
        {
            var test = new ComplexTestObject
            {
                IntegerValue = 4,
                Child = new ComplexChildObject
                {
                    Text = "Test"
                }
            };

            var serialized = serializer.SerializeToString(test);
            var newObj = serializer.Deserialize<ComplexTestObject>(serialized);

            Assert.AreEqual(test.IntegerValue, newObj.IntegerValue);
            Assert.AreEqual(test.Child.Text, newObj.Child.Text);
        }

        [TestMethod]
        public void TestSkipCycle()
        {
            var test = new ComplexTestObject
            {
                Child = new ComplexChildObject()
            };
            test.Child.Parent = test;

            var serialized = serializer.SerializeToString(test);
            var newObj = serializer.Deserialize<ComplexTestObject>(serialized);

            Assert.AreEqual(newObj, newObj.Child.Parent);
            Assert.AreEqual(newObj.Child, newObj.Child.Parent.Child);
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

        [TestMethod]
        public void TestNull()
        {
            var test = new DirectCycleTestObject
            {
                Child = null
            };

            var testString = serializer.SerializeToString(test);
            var newTestObject = serializer.Deserialize<DirectCycleTestObject>(testString);

            Assert.AreEqual(null, newTestObject.Child);
        }

        [TestMethod]
        public void TestBackingField()
        {
            var test = new BackingFieldTestObject("BackingFieldTest");
            var testString = serializer.SerializeToString(test);
            var newTestObject = serializer.Deserialize<BackingFieldTestObject>(testString);

            Assert.AreEqual(test.Value, newTestObject.Value);
        }
    }
}