using System;
using System.IO;
using System.Xml;

namespace Marked.Serializer
{
    public class XmlDataWriter : IDataWriter
    {
        public Stream Stream
        {
            get => stream;
            set
            {
                stream = value;
                writer = XmlWriter.Create(stream);
                writer.WriteStartDocument();
            }
        }

        private Stream stream;
        private XmlWriter writer;


        public XmlDataWriter() { }
        public XmlDataWriter(Stream stream)
        {
            Stream = stream;
        }

        public void Dispose()
        {
            writer.WriteEndDocument();
            writer.Close();
        }

        public void WriteReference(int refId)
        {
            writer.WriteAttributeString("RefId", refId.ToString());
        }

        public void WriteId(int id)
        {
            writer.WriteAttributeString("Id", id.ToString());
        }

        public void WriteType(Type type)
        {
            writer.WriteAttributeString("Type", $"{type}, {type.Assembly.GetName().Name}");
        }

        public void WriteStartNode(string name)
        {
            writer.WriteStartElement(name);
        }

        public void WriteContent(object content)
        {
            writer.WriteValue(content);
        }

        public void WriteEndNode(string name)
        {
            writer.WriteEndElement();
        }

        public void WriteArrayLength(long length)
        {
            writer.WriteAttributeString("Count", length.ToString());
        }
    }
}
