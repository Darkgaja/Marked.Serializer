using System;
using System.Collections.Generic;

namespace Marked.Serializer
{
    public static class SerializerFactory
    {
        private static readonly Dictionary<Type, ISerializer> serializers 
            = new Dictionary<Type, ISerializer>();

        public static ISerializer Get(Type type)
        {
            // try to get an existing generic serializer for that type
            // if everything else fails, create a new one
            if (!GetExistingSerializer(type, out var serializer))
            {
                if (PrimitiveSerializer.IsValid(type))
                {
                    serializer = new PrimitiveSerializer(type);
                }
                else if (CollectionSerializer.IsValid(type))
                {
                    serializer = new CollectionSerializer(type);
                }
                else
                {
                    serializer = new ObjectSerializer(type);
                }
                serializers.Add(type, serializer);
            }
            return serializer;
        }

        private static bool GetExistingSerializer(Type type, out ISerializer serializer)
        {
            return serializers.TryGetValue(type, out serializer);
        }
    }
}