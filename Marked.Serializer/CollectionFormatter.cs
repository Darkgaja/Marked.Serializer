using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Marked.Serializer
{
    public class CollectionFormatter : IFormatter
    {
        public Type Type { get; }
        private Action<IDataWriter, object> writeAction;
        private Func<IDataReader, object, object> readAction;

        private delegate object SetterFunction();

        private const string ItemName = "Item";

        public CollectionFormatter(Type type)
        {
            Type = type;
            Initialize();
        }

        public object Read(IDataReader reader, object o)
        {
            return readAction(reader, o);
        }

        public void Write(IDataWriter writer, object o)
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

        private void WriteArray(IDataWriter writer, object o)
        {
            Array array = o as Array;

            writer.WriteArrayLength(array.LongLength);

            for (int i = 0; i < array.Length; i++)
            {
                SerializeItem(writer, array.GetValue(i));
            }
        }

        private object ReadArray(IDataReader reader, object o)
        {
            List<SetterFunction> objectGetters = new List<SetterFunction>();

            long arrayLength = reader.ReadArrayLength();

            for (long i = 0; i < arrayLength; i++)
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

        private void WriteList(IDataWriter writer, object o)
        {
            var list = o as IList;

            writer.WriteArrayLength(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                SerializeItem(writer, list[i]);
            }
        }

        private object ReadList(IDataReader reader, object o)
        {
            IList list = (IList)Activator.CreateInstance(Type);

            var cycleUtility = CycleUtility.GetInstance(reader);
            
            long arrayLength = reader.ReadArrayLength();

            for (long i = 0; i < arrayLength; i++)
            {
                var func = DeserializeItem(reader);
                cycleUtility.AddCycleSetter(() => list.Add(func()));
            }
            
            return list;
        }

        private void WriteDictionary(IDataWriter writer, object o)
        {
            var dictionary = o as IDictionary;

            writer.WriteArrayLength(dictionary.Count);

            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];
                writer.WriteStartNode(ItemName);
                SerializeItem(writer, key, "Key");
                SerializeItem(writer, value, "Value");
                writer.WriteEndNode(ItemName);
            }
        }

        private object ReadDictionary(IDataReader reader, object o)
        {
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(Type);

            long arrayLength = reader.ReadArrayLength();

            for (long i = 0; i < arrayLength; i++)
            {
                reader.ReadStartNode(ItemName);
                var key = DeserializeItem(reader, "Key");
                var value = DeserializeItem(reader, "Value");
                reader.ReadEndNode(ItemName);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        private void SerializeItem(IDataWriter writer, object item, string name = ItemName)
        {
            writer.WriteStartNode(name);
            if (item != null)
            {
                var type = item.GetType();
                var formatter = FormatterFactory.Get(type);
                writer.WriteType(type);
                formatter.Write(writer, item);
            }
            writer.WriteEndNode(name);
        }

        private SetterFunction DeserializeItem(IDataReader reader, string name = ItemName)
        {
            if (!reader.IsEmptyElement)
            {
                reader.ReadStartNode(name);
                var formatter = FormatterFactory.Get(reader.ReadType());
                object value = formatter.Read(reader, null);
                reader.ReadEndNode(name);
                return () => value;
            }
            else
            {
                var func = ReadCyclicObject(reader);
                reader.ReadEmptyNode(name);
                return func;
            }
        }

        private SetterFunction ReadCyclicObject(IDataReader reader)
        {
            var cycleUtility = CycleUtility.GetInstance(reader);
            int refId = reader.ReadReference();
            if (refId >= 0)
            {
                return () => cycleUtility.FromReference(refId);
            }
            else
            {
                return null;
            }
        }

        public static bool IsValid(Type type)
        {
            return type.IsSubclassOf(typeof(Array)) ||
                typeof(IList).IsAssignableFrom(type) ||
                typeof(IDictionary).IsAssignableFrom(type);
        }
    }
}