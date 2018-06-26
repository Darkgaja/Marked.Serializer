using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializerIgnoreAttribute : Attribute
    {
    }
}