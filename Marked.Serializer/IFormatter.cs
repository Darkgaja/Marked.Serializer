using System;
using System.Xml;

namespace Marked.Serializer
{
    public interface IFormatter
    {
        Type Type { get; }
        void Write(IDataWriter writer, object o);
        object Read(IDataReader reader, object o);
    }
}