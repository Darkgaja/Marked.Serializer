using System;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Marked.Serializer
{
    public class PrimitiveFormatter : IFormatter
    {
        private static Type[] SupportedTypes { get; } = new Type[]
            {
                typeof(bool),
                typeof(uint),
                typeof(int),
                typeof(byte),
                typeof(sbyte),
                typeof(ushort),
                typeof(short),
                typeof(ulong),
                typeof(long),
                typeof(decimal),
                typeof(float),
                typeof(double),
                typeof(string),
                typeof(DateTime),
                typeof(DateTimeOffset)
            };

        public Type Type { get; }

        public PrimitiveFormatter(Type type)
        {
            if (!IsValid(type))
                throw new InvalidCastException($"{type.Name} is not a primitive type");

            Type = type;
        }

        public object Read(IDataReader reader, object o)
        {
            return reader.ReadContent(Type);
        }

        public void Write(IDataWriter writer, object o)
        {
            writer.WriteContent(o);
        }

        public static bool IsValid(Type type)
        {
            if (type.IsPrimitive)
                return true;
            if (type.IsEnum)
                return true;
            return SupportedTypes.Contains(type);
        }
    }
}