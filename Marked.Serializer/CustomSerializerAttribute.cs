using System;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | 
                    AttributeTargets.Class | AttributeTargets.Struct)]
    public class CustomFormatterAttribute : Attribute
    {
        public IFormatter Formatter
        {
            get
            {
                if (formatter == null)
                {
                    formatter = Activator.CreateInstance(FormatterType) as IFormatter;
                }
                return formatter;
            }
        }
        public Type FormatterType { get; set; }

        private IFormatter formatter;

        public CustomFormatterAttribute(Type formatterType)
        {
            FormatterType = formatterType;
        }
    }
}