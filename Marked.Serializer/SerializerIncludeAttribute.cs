using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializerIncludeAttribute : Attribute
    {
        public string Name { get; set; }
    }
}