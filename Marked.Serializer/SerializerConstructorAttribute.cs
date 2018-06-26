using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class SerializerConstructorAttribute : Attribute
    {
        public string[] Parameters { get; }

        public SerializerConstructorAttribute(params string[] parameters)
        {
            Parameters = parameters;
        }
    }
}