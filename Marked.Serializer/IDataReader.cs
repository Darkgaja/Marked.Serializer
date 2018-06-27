using System;
using System.IO;

namespace Marked.Serializer
{
    public interface IDataReader : IDisposable
    {
        Stream Stream { get; set; }
        bool IsEmptyElement { get; }
        int ReadReference();
        int ReadId();
        Type ReadType();
        long ReadArrayLength();
        bool ReadStartNode(string name);
        bool ReadEmptyNode(string name);
        object ReadContent(Type type);
        bool ReadEndNode(string name);
    }
}