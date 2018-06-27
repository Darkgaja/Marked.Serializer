using System;
using System.IO;
using System.Xml;

namespace Marked.Serializer
{
    public class XmlDataReader : IDataReader
    {
        public Stream Stream
        {
            get => stream;
            set
            {
                stream = value;
                reader = XmlReader.Create(stream);
                reader.MoveToContent();
            }
        }

        public bool IsEmptyElement => reader.IsEmptyElement;

        private Stream stream;
        private XmlReader reader;

        public XmlDataReader() { }
        public XmlDataReader(Stream stream)
        {
            Stream = stream;
        }

        public void Dispose()
        {
            reader.Close();
        }

        public long ReadArrayLength()
        {
            return long.Parse(reader.GetAttribute("Count"));
        }

        public object ReadContent(Type type)
        {
            if (type.IsEnum)
            {
                string enumString = reader.ReadContentAsString();
                return Enum.Parse(type, enumString);
            }
            else
            {
                return reader.ReadContentAs(type, null);
            }
        }

        public bool ReadEndNode(string name)
        {
            if (reader.LocalName == name)
            {
                reader.ReadEndElement();
                return true;
            }
            return false;
        }

        public int ReadId()
        {
            return int.TryParse(reader.GetAttribute("Id"), out int id) ? id : -1;
        }

        public int ReadReference()
        {
            return int.TryParse(reader.GetAttribute("RefId"), out int id) ? id : -1;
        }

        public bool ReadStartNode(string name)
        {
            if (reader.IsStartElement() && reader.LocalName == name)
            {
                reader.ReadStartElement(name);
                return true;
            }
            else
            {
                return false;
            }
        }

        public Type ReadType()
        {
            return Type.GetType(reader.GetAttribute("Type"));
        }

        public bool ReadEmptyNode(string name)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return true;
            }
            return false;
        }
    }
}