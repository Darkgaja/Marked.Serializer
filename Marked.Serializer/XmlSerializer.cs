using System;
using System.IO;
using System.Text;

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

            using (XmlDataWriter writer = new XmlDataWriter(stream))
            {
                Serialize(writer, obj);
            }
        }

        public void Serialize(IDataWriter writer, object obj)
        {
            try
            {
                if (writer == null)
                    throw new ArgumentNullException(nameof(writer));
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));

                var type = obj.GetType();
                var formatter = FormatterFactory.Get(type);
                writer.WriteStartNode(type.Name);
                formatter.Write(writer, obj);
                writer.WriteEndNode(type.Name);
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

        public T Deserialize<T>(IDataReader reader)
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
            using (XmlDataReader reader = new XmlDataReader(stream))
            {
                return Deserialize<T>(reader, obj);
            }
        }

        public T Deserialize<T>(IDataReader reader, object obj)
        {
            object value = null;
            var type = typeof(T);

            if (!reader.IsEmptyElement)
            {
                var formatter = FormatterFactory.Get(type);
                int id = reader.ReadId();
                reader.ReadStartNode(type.Name);
                value = formatter.Read(reader, obj);
                if (id > -1)
                {
                    CycleUtility.GetInstance(reader).AddReference(id, value);
                }
                reader.ReadEndNode(type.Name);
            }
            CycleUtility.RemoveInstance(reader);

            return (T)value;
        }
    }
}