using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | 
                    AttributeTargets.Class | AttributeTargets.Struct)]
    public class CustomSerializerAttribute : Attribute
    {
        public ISerializer Serializer => (ISerializer)Activator.CreateInstance(SerializerType);
        public Type SerializerType { get; set; }

        public CustomSerializerAttribute(Type type)
        {
            SerializerType = type;
        }
    }
}