using System.Collections.Generic;

namespace Marked.Serializer.Test
{
    public class PrimitiveTestObject
    {
        public int IntegerValue { get; set; }
        public string StringValue { get; set; }
        public float SingleValue { get; set; }
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


}