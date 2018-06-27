using System.Collections.Generic;

namespace Marked.Serializer.Test
{
    public class PrimitiveTestObject
    {
        public int IntegerValue { get; set; }
        public string StringValue { get; set; }
        public float SingleValue { get; set; }
    }

    public class ComplexTestObject
    {
        public int IntegerValue { get; set; }
        public ComplexChildObject Child { get; set; }
    }

    public class ComplexChildObject
    {
        public string Text { get; set; }
        public ComplexTestObject Parent { get; set; }
    }

    public class DirectCycleTestObject
    {
        public int Value { get; set; }
        public DirectCycleTestObject Child { get; set; }
    }

    public class ListCycleTestObject
    {
        public int Value { get; set; }
        public List<ListCycleTestObject> Children { get; set; }
    }

    public class BackingFieldTestObject
    {
        [SerializerUseBackingField]
        public string Value { get; }

        public BackingFieldTestObject()
        {
            Value = null;
        }

        public BackingFieldTestObject(string value)
        {
            Value = value;
        }
    }
}