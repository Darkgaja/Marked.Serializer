using System;
using System.Xml;

namespace Marked.Serializer
{
    public interface ISerializer
    {
        Type Type { get; }
        void Write(XmlWriter writer, object o);
        object Read(XmlReader reader, object o);
    }
}