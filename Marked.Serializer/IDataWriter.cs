using System;
using System.IO;

namespace Marked.Serializer
{
    public interface IDataWriter : IDisposable
    {
        Stream Stream { get; set; }
        void WriteReference(int refId);
        void WriteId(int id);
        void WriteType(Type type);
        void WriteArrayLength(long length);
        void WriteStartNode(string name);
        void WriteContent(object content);
        void WriteEndNode(string name);
    }
}