using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Marked.Serializer
{
    public class XmlSerializer
    {
        public string SerializeToString(object obj)
        {
            string value;
            using (var ms = new MemoryStream())
            {
                Serialize(ms, obj);
                ms.Position = 0;
                using (StreamReader reader = new StreamReader(ms))
                {
                    value = reader.ReadToEnd();
                }
            }
            return value;
        }

        public void Serialize(Stream stream, object obj)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            using (XmlWriter writer = XmlWriter.Create(stream))
            {
                Serialize(writer, obj);
            }
        }

        public void Serialize(XmlWriter writer, object obj)
        {
            try
            {
                if (writer == null)
                    throw new ArgumentNullException(nameof(writer));
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));

                var type = obj.GetType();
                var serializer = SerializerFactory.Get(type);
                writer.WriteStartDocument();
                writer.WriteStartElement(type.Name);
                serializer.Write(writer, obj);
                writer.WriteEndElement();
                writer.WriteEndDocument();
                CycleUtility.RemoveInstance(writer);
            }
            catch
            {

            }
        }

        public T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            return Deserialize<T>(xml, null);
        }

        public T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return Deserialize<T>(stream, null);
        }

        public T Deserialize<T>(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return Deserialize<T>(reader, null);
        }

        public T Deserialize<T>(string xml, object obj)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return Deserialize<T>(ms, obj);
            }
        }

        public T Deserialize<T>(Stream stream, object obj)
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                return Deserialize<T>(reader, obj);
            }
        }

        public T Deserialize<T>(XmlReader reader, object obj)
        {
            object value = null;
            try
            {
                if (!reader.IsEmptyElement)
                {
                    var serializer = SerializerFactory.Get(typeof(T));
                    reader.MoveToContent();
                    int id = int.TryParse(reader.GetAttribute("Id"), out int refId) ? refId : -1;
                    reader.ReadStartElement();
                    value = serializer.Read(reader, obj);
                    if (id > -1)
                    {
                        CycleUtility.GetInstance(reader).AddReference(id, value);
                    }
                    reader.ReadEndElement();
                }
                CycleUtility.RemoveInstance(reader);
            }
            catch (Exception e)
            {

            }
            return (T)value;
        }
    }
}