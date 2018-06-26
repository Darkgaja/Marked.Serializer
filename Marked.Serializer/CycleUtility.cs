using System;
using System.Collections.Generic;
using System.Xml;

namespace Marked.Serializer
{
    public class CycleUtility
    {
        private static readonly Dictionary<XmlReader, CycleUtility> readerCycles
            = new Dictionary<XmlReader, CycleUtility>();
        private static readonly Dictionary<XmlWriter, CycleUtility> writerCycles
            = new Dictionary<XmlWriter, CycleUtility>();
        
        private int id = 0;

        private readonly Dictionary<object, int> ids
            = new Dictionary<object, int>();
        private readonly Dictionary<int, object> objects
            = new Dictionary<int, object>();

        private readonly List<Action> setterActions 
            = new List<Action>();

        private CycleUtility()
        {
        }

        public static bool ValidCycleType(Type type)
        {
            return type.IsClass;
        }

        public bool TryGetReferenceId(object obj, out int refId)
        {
            if (!ids.TryGetValue(obj, out refId))
            {
                refId = id;
                ids.Add(obj, id++);
                return false;
            }
            return true;
        }

        public void AddReference(int id, object obj)
        {
            objects[id] = obj;
        }

        public object FromReference(int refId)
        {
            return objects[refId];
        }

        public static CycleUtility GetInstance(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (!readerCycles.TryGetValue(reader, out var cycleUtility))
            {
                cycleUtility = new CycleUtility();
                readerCycles.Add(reader, cycleUtility);
            }
            return cycleUtility;
        }

        public static CycleUtility GetInstance(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (!writerCycles.TryGetValue(writer, out var cycleUtility))
            {
                cycleUtility = new CycleUtility();
                writerCycles.Add(writer, cycleUtility);
            }
            return cycleUtility;
        }

        public void AddCycleSetter(Action setter)
        {
            setterActions.Add(setter);
        }

        public void AddCycleSetters(IEnumerable<Action> setters)
        {
            setterActions.AddRange(setters);
        }

        public void ExecuteCycleSetters()
        {
            foreach (var setter in setterActions)
            {
                setter.Invoke();
            }
        }

        public static void RemoveInstance(XmlWriter writer)
        {
            GetInstance(writer)?.ExecuteCycleSetters();
            writerCycles.Remove(writer ?? throw new ArgumentNullException(nameof(writer)));
        }

        public static void RemoveInstance(XmlReader reader)
        {
            GetInstance(reader)?.ExecuteCycleSetters();
            readerCycles.Remove(reader ?? throw new ArgumentNullException(nameof(reader)));
        }
    }
}