using System;
using System.IO;

namespace Marked.Serializer
{
    public interface IDataWriter : IDisposable
    {
        Stream Stream { get; set; }
        void WriteReferenceNode(string name, Type type, int id);
        void WriteValueNode(string name, Type type);
        void WriteContent(object content);
        void WriteEndNode(string name);
    }
}