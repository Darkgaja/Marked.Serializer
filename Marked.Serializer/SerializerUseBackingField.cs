using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marked.Serializer
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializerUseBackingField : Attribute
    {
    }
}