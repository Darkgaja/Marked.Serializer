using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Marked.Serializer
{
    public class CollectionSerializer : ISerializer
    {
        public Type Type { get; }
        private Action<XmlWriter, object> writeAction;
        private Func<XmlReader, object, object> readAction;

        private delegate object SetterFunction();

        public CollectionSerializer(Type type)
        {
            Type = type;
            Initialize();
        }

        public object Read(XmlReader reader, object o)
        {
            return readAction(reader, o);
        }

        public void Write(XmlWriter writer, object o)
        {
            writeAction(writer, o);
        }

        public void Initialize()
        {
            if (Type.IsSubclassOf(typeof(Array)))
            {
                writeAction = WriteArray;
                readAction = ReadArray;
            }
            else if (typeof(IList).IsAssignableFrom(Type) && Type.IsGenericType)
            {
                writeAction = WriteList;
                readAction = ReadList;
            }
            else if (typeof(IDictionary).IsAssignableFrom(Type) && Type.IsGenericType)
            {
                writeAction = WriteDictionary;
                readAction = ReadDictionary;
            }
        }

        private void WriteArray(XmlWriter writer, object o)
        {
            Array array = o as Array;

            for (int i = 0; i < array.Length; i++)
            {
                SerializeItem(writer, array.GetValue(i));
            }
        }

        private object ReadArray(XmlReader reader, object o)
        {
            List<SetterFunction> objectGetters = new List<SetterFunction>();

            while (reader.IsStartElement())
            {
                objectGetters.Add(DeserializeItem(reader));
            }

            Array array = (Array)Activator.CreateInstance(Type, objectGetters.Count);

            var cycleUtility = CycleUtility.GetInstance(reader);
            cycleUtility.AddCycleSetter(setArray);

            return array;

            void setArray()
            {
                for (int i = 0; i < objectGetters.Count; i++)
                {
                    array.SetValue(objectGetters[i].Invoke(), i);
                }
            }
        }

        private void WriteList(XmlWriter writer, object o)
        {
            var list = o as IList;

            for (int i = 0; i < list.Count; i++)
            {
                SerializeItem(writer, list[i]);
            }
        }

        private object ReadList(XmlReader reader, object o)
        {
            IList list = (IList)Activator.CreateInstance(Type);

            var cycleUtility = CycleUtility.GetInstance(reader);

            while (reader.IsStartElement())
            {
                var func = DeserializeItem(reader);
                cycleUtility.AddCycleSetter(() => list.Add(func()));
            }
            
            return list;
        }

        private void WriteDictionary(XmlWriter writer, object o)
        {
            var dictionary = o as IDictionary;

            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];
                writer.WriteStartElement("Item");
                SerializeItem(writer, key, "Key");
                SerializeItem(writer, value, "Value");
                writer.WriteEndElement();
            }
        }

        private object ReadDictionary(XmlReader reader, object o)
        {
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(Type);

            while (reader.IsStartElement())
            {
                reader.ReadStartElement();
                var key = DeserializeItem(reader);
                var value = DeserializeItem(reader);
                reader.ReadEndElement();

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        private void SerializeItem(XmlWriter writer, object item, string name = "Item")
        {
            writer.WriteStartElement(name);
            if (item != null)
            {
                var type = item.GetType();
                var serializer = SerializerFactory.Get(type);
                writer.WriteAttributeString("Type", $"{type}, {type.Assembly.GetName().Name}");
                serializer.Write(writer, item);
            }
            writer.WriteEndElement();
        }

        private SetterFunction DeserializeItem(XmlReader reader)
        {
            if (!reader.IsEmptyElement)
            {
                string typeString = reader.GetAttribute("Type");
                reader.ReadStartElement();
                var serializer = SerializerFromTypeString(typeString);
                object value = serializer.Read(reader, null);
                reader.ReadEndElement();
                return () => value;
            }
            else
            {
                var func = ReadCyclicObject(reader);
                reader.Read();
                return func;
            }
        }

        private SetterFunction ReadCyclicObject(XmlReader reader)
        {
            var cycleUtility = CycleUtility.GetInstance(reader);
            string refIdString = reader.GetAttribute("RefId");
            if (int.TryParse(refIdString, out int id))
            {
                return () => cycleUtility.FromReference(id);
            }
            else
            {
                return null;
            }
        }

        private ISerializer SerializerFromTypeString(string typeString)
        {
            return SerializerFactory.Get(Type.GetType(typeString));
        }

        public static bool IsValid(Type type)
        {
            return type.IsSubclassOf(typeof(Array)) ||
                typeof(IList).IsAssignableFrom(type) ||
                typeof(IDictionary).IsAssignableFrom(type);
        }
    }
}