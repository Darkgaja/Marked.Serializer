using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;

namespace Marked.Serializer
{
    public class ObjectSerializer : ISerializer
    {
        public Type Type { get; }

        private readonly Constructor constructor;
        private readonly Field[] fields;

        private delegate void SetterMethod(object obj);
        private delegate void ObjectSetter(object obj, object value);

        public ObjectSerializer(Type type)
        {
            Type = type;
            fields = GetFields(Type);
            constructor = GetDefaultConstructor();
        }

        private ISerializer SerializerFromTypeString(string typeString, out Type type)
        {
            type = Type.GetType(typeString);
            return SerializerFromType(type);
        }

        private ISerializer SerializerFromType(Type type)
        {
            return SerializerFactory.Get(type);
        }

        private ISerializer SerializerFromReader(XmlReader reader, out Type type)
        {
            string typeString = reader.GetAttribute("Type");
            return SerializerFromTypeString(typeString, out type);
        }

        public object Read(XmlReader reader, object o)
        {
            var values = new Dictionary<string, object>();
            var cycleActions = new List<SetterMethod>();

            foreach (var field in fields)
            {
                if (field.HasGetter())
                {
                    object existingValue = o == null ? null : field.Get(o);
                    var action = Read(reader, field, existingValue, values);
                    if (action != null)
                    {
                        cycleActions.Add(action);
                    }
                }
            }

            if (o == null)
            {
                if (constructor == null)
                {
                    o = Activator.CreateInstance(Type);
                }
                else
                {
                    o = constructor.Info.Invoke(GetConstructorParameters(values, constructor.Attribute));
                }
            }

            foreach (var field in fields)
            {
                if (field.HasSetter())
                {
                    var value = values[field.Name];
                    field.Set(o, value);
                }
            }

            if (cycleActions.Count > 0)
            {
                var cycleUtility = CycleUtility.GetInstance(reader);
                cycleUtility.AddCycleSetters(cycleActions.Select(e => (Action)(() => e.Invoke(o))));
            }

            return o;
        }
        
        private SetterMethod Read(XmlReader reader, Field field, object value, Dictionary<string, object> values)
        {
            string name = field.Name;
            if (!reader.IsEmptyElement)
            {
                var serializerAttribute = field.Member.GetCustomAttribute<CustomSerializerAttribute>();
                var serializer = serializerAttribute?.Serializer ?? SerializerFromReader(reader, out var type);
                int.TryParse(reader.GetAttribute("Id"), out int cycleId);

                reader.ReadStartElement(name);
                object cycleValue = values[name] = serializer.Read(reader, value);
                if (CycleUtility.ValidCycleType(serializer.Type))
                {
                    CycleUtility.GetInstance(reader).AddReference(cycleId, cycleValue);
                }
                reader.ReadEndElement();
                return null;
            }
            else
            {
                SetterMethod cycleReader = ReadCyclicObject(reader, field.Set);
                values[name] = null;
                reader.Read();
                return cycleReader;
            }
        }

        private SetterMethod ReadCyclicObject(XmlReader reader, ObjectSetter setter)
        {
            var cycleUtility = CycleUtility.GetInstance(reader);
            string refIdString = reader.GetAttribute("RefId");
            if (int.TryParse(refIdString, out int id))
            {
                return (obj) => setter(obj, cycleUtility.FromReference(id));
            }
            else
            {
                return null;
            }
        }

        public void Write(XmlWriter writer, object o)
        {
            if (CycleUtility.ValidCycleType(o.GetType()))
            {
                var cycleUtil = CycleUtility.GetInstance(writer);
                if (cycleUtil.TryGetReferenceId(o, out int id))
                {
                    writer.WriteAttributeString("RefId", id.ToString());
                    return;
                }
                else
                {
                    writer.WriteAttributeString("Id", id.ToString());
                }
            }
            foreach (var field in fields)
            {
                if (field.HasGetter())
                {
                    var fieldValue = field.Get(o);
                    Write(writer, field, fieldValue);
                }
            }
        }

        private void Write(XmlWriter writer, Field field, object value)
        {
            string name = field.Name;
            writer.WriteStartElement(name);
            if (value != null)
            {
                var type = value.GetType();
                var serializerAttribute = field.Member.GetCustomAttribute<CustomSerializerAttribute>();
                var serializer = serializerAttribute?.Serializer ?? SerializerFactory.Get(type);

                WriteTypeString(writer, type);
                serializer.Write(writer, value);
            }
            writer.WriteEndElement();
        }

        private void WriteTypeString(XmlWriter writer, Type type)
        {
            writer.WriteAttributeString("Type", $"{type}, {type.Assembly.GetName().Name}");
        }

        private Constructor GetDefaultConstructor()
        {
            var publicConstructors = Type.GetConstructors();

            var defaultConstructor = publicConstructors.SingleOrDefault(e => e.GetCustomAttribute<SerializerConstructorAttribute>() != null);
            if (defaultConstructor == null)
            {
                defaultConstructor = Type.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor == null)
                {
                    return null;
                }
                return new Constructor(defaultConstructor, null);
            }

            var attr = defaultConstructor.GetCustomAttribute<SerializerConstructorAttribute>();
            return new Constructor(defaultConstructor, attr);
        }

        private Field[] GetFields(Type type)
        {
            if (type == typeof(object) || type == null)
                return new Field[0];

            var publicFields = type.GetFields();
            var privateFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            var publicProperties = type.GetProperties();
            var privateProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

            IEnumerable<FieldInfo> noIgnoreFields;
            IEnumerable<PropertyInfo> noIgnoreProperties;
            if (type != Type || type.GetCustomAttribute(typeof(SerializerForceIncludeAttribute)) != null)
            {
                noIgnoreFields = privateFields.Concat(publicFields).Where(e => e.GetCustomAttribute(typeof(SerializerIncludeAttribute)) != null);
                noIgnoreProperties = privateProperties.Concat(publicProperties).Where(e => e.GetCustomAttribute<SerializerIncludeAttribute>() != null);
            }
            else
            {
                var privateIncludeFields = privateFields.Where(e => e.GetCustomAttribute(typeof(SerializerIncludeAttribute)) != null);
                var allFields = publicFields.Concat(privateIncludeFields);

                noIgnoreFields = allFields.Where(e => e.GetCustomAttribute(typeof(SerializerIgnoreAttribute)) == null);

                var privateIncludeProperties = privateProperties.Where(e => e.GetCustomAttribute<SerializerIncludeAttribute>() != null);
                var allProperties = publicProperties.Concat(privateIncludeProperties);

                noIgnoreProperties = allProperties.Where(e => e.GetCustomAttribute<SerializerIgnoreAttribute>() == null);
            }

            return noIgnoreFields.Concat(noIgnoreProperties.Cast<MemberInfo>()).Select(e => new Field(e, e.GetCustomAttribute<SerializerIncludeAttribute>())).Concat(GetFields(type.BaseType)).ToArray();
        }

        private object[] GetConstructorParameters(Dictionary<string, object> values, SerializerConstructorAttribute constructorAttribute)
        {
            var parameters = new object[constructorAttribute?.Parameters?.Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = values[constructorAttribute.Parameters[i]];
            }
            return parameters;
        }

        private FieldInfo GetBackingField(PropertyInfo propertyInfo)
        {
            return propertyInfo.DeclaringType.GetField($"<{propertyInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private class Constructor
        {
            public SerializerConstructorAttribute Attribute { get; set; }
            public ConstructorInfo Info { get; set; }

            public Constructor(ConstructorInfo info, SerializerConstructorAttribute attribute)
            {
                Info = info;
                Attribute = attribute;
            }
        }

        private class Field
        {
            public SerializerIncludeAttribute Attribute { get; set; }
            public MemberInfo Member { get; set; }

            public string Name => Attribute?.Name ?? Member.Name; //Attribute == null ? Member.Name : Attribute.Name ?? Member.Name;

            public Field(MemberInfo member, SerializerIncludeAttribute attribute)
            {
                Member = member;
                Attribute = attribute;
            }

            public bool HasSetter()
            {
                if (Member is FieldInfo field)
                    return true;
                if (Member is PropertyInfo property)
                    return property.GetSetMethod() != null;
                return false;
            }

            public bool HasGetter()
            {
                if (Member is FieldInfo field)
                    return true;
                if (Member is PropertyInfo property)
                    return property.GetGetMethod() != null;
                return false;
            }

            public void Set(object obj, object value)
            {
                if (Member is FieldInfo field)
                    field.SetValue(obj, value);
                if (Member is PropertyInfo property)
                    property.SetValue(obj, value);
            }

            public object Get(object obj)
            {
                if (Member is FieldInfo field)
                    return field.GetValue(obj);
                if (Member is PropertyInfo property)
                    return property.GetValue(obj);

                throw new InvalidCastException();
            }
        }
        
    }
}