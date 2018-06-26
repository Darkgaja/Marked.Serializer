using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public sealed class SerializerForceIncludeAttribute : Attribute
    {
    }
}
