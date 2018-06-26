using System;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Marked.Serializer
{
    public class PrimitiveSerializer : ISerializer
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

        public PrimitiveSerializer(Type type)
        {
            if (!IsValid(type))
                throw new InvalidCastException($"{type.Name} is not a primitive type");

            Type = type;
        }

        public void Initialize()
        {

        }

        public object Read(XmlReader reader, object o)
        {
            if (Type.IsEnum)
            {
                string enumString = reader.ReadContentAsString();
                return Enum.Parse(Type, enumString);
            }
            else
            {
                return reader.ReadContentAs(Type, null);
            }
        }

        public void Write(XmlWriter writer, object o)
        {
            if (o is IFormattable formattable)
            {
                writer.WriteValue(formattable.ToString(null, NumberFormatInfo.InvariantInfo));
            }
            else
            {
                writer.WriteValue(o);
            }
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