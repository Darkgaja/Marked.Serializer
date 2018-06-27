using System;
using System.IO;
using System.Text;

namespace Marked.Serializer
{
    public class BinaryDataWriter : IDataWriter
    {
        public Stream Stream
        {
            get => writer.BaseStream;
            set
            {
                writer = new BinaryWriter(value, Encoding.UTF8, true);
            }
        }

        private BinaryWriter writer;

        public BinaryDataWriter() { }
        public BinaryDataWriter(Stream stream)
        {
            Stream = stream;
        }

        public void Dispose()
        {
            writer.Close();
            writer.Dispose();
        }

        public void WriteArrayLength(long length)
        {
            Write(BinaryDataToken.ArrayLength);
            writer.Write(length);
        }

        public void WriteContent(object content)
        {
            Write(BinaryDataToken.Content);
            writer.Write(content.ToString());
        }

        public void WriteEndNode(string name)
        {
            Write(BinaryDataToken.NodeEnd);
            writer.Write(name);
        }

        public void WriteId(int id)
        {
            Write(BinaryDataToken.Id);
            writer.Write(id);
        }

        public void WriteReference(int refId)
        {
            Write(BinaryDataToken.RefId);
            writer.Write(refId);
        }

        public void WriteStartNode(string name)
        {
            Write(BinaryDataToken.NodeStart);
            writer.Write(name);
        }

        public void WriteType(Type type)
        {
            Write(BinaryDataToken.Type);
            writer.Write($"{type}, {type.Assembly.GetName().Name}");
        }

        private void Write(BinaryDataToken binaryDataToken)
        {
            writer.Write((byte)binaryDataToken);
        }
    }
}
