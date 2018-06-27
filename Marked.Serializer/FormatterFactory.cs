using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Marked.Serializer
{
    public static class FormatterFactory
    {
        private static readonly Dictionary<Type, IFormatter> formatters 
            = new Dictionary<Type, IFormatter>();

        public static IFormatter Get(Type type)
        {
            // try to get an existing generic serializer for that type
            // if everything else fails, create a new one
            if (!GetExistingFormatter(type, out var formatter))
            {
                if (PrimitiveFormatter.IsValid(type))
                {
                    formatter = new PrimitiveFormatter(type);
                }
                else if (CollectionFormatter.IsValid(type))
                {
                    formatter = new CollectionFormatter(type);
                }
                else
                {
                    formatter = new ComplexTypeFormatter(type);
                }
                formatters.Add(type, formatter);
            }
            return formatter;
        }

        private static bool GetExistingFormatter(Type type, out IFormatter serializer)
        {
            return formatters.TryGetValue(type, out serializer);
        }
    }
}