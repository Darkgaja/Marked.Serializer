using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Runtime.Serialization;

namespace Marked.Serializer
{
    public class ComplexTypeFormatter : IFormatter
    {
        public Type Type { get; }

        private readonly Constructor constructor;
        private readonly ObjectMember[] members;

        private delegate void SetterMethod(object obj);
        private delegate void ObjectSetter(object obj, object value);

        public ComplexTypeFormatter(Type type)
        {
            Type = type;
            members = GetObjectMembers(Type);
            constructor = GetDefaultConstructor();
        }

        private IFormatter FormatterFromReader(IDataReader reader, out Type type)
        {
            return FormatterFactory.Get(type = reader.ReadType());
        }

        public object Read(IDataReader reader, object o)
        {
            var values = new Dictionary<string, object>();
            var cycleActions = new List<SetterMethod>();

            foreach (var field in members)
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
                    o = FormatterServices.GetSafeUninitializedObject(Type);
                }
                else
                {
                    o = constructor.Info.Invoke(GetConstructorParameters(values, constructor.Attribute));
                }
            }

            foreach (var field in members)
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
        
        private SetterMethod Read(IDataReader reader, ObjectMember field, object value, Dictionary<string, object> values)
        {
            string name = field.Name;
            if (!reader.IsEmptyElement)
            {
                var serializerAttribute = field.Member.GetCustomAttribute<CustomFormatterAttribute>(false);
                var serializer = serializerAttribute?.Formatter ?? FormatterFromReader(reader, out var type);
                int cycleId = reader.ReadId();

                reader.ReadStartNode(name);
                object cycleValue = values[name] = serializer.Read(reader, value);
                if (CycleUtility.ValidCycleType(serializer.Type))
                {
                    CycleUtility.GetInstance(reader).AddReference(cycleId, cycleValue);
                }
                reader.ReadEndNode(name);
                // no setter method required
                return null;
            }
            else
            {
                SetterMethod referenceReader = ReadReferencedObject(reader, field.Set);
                values[name] = null;
                reader.ReadEmptyNode(field.Name);
                return referenceReader;
            }
        }

        private SetterMethod ReadReferencedObject(IDataReader reader, ObjectSetter setter)
        {
            var cycleUtility = CycleUtility.GetInstance(reader);
            int id = reader.ReadReference();
            if (id >= 0)
            {
                return (obj) => setter(obj, cycleUtility.FromReference(id));
            }
            else
            {
                return null;
            }
        }

        public void Write(IDataWriter writer, object o)
        {
            if (CycleUtility.ValidCycleType(o.GetType()))
            {
                var cycleUtil = CycleUtility.GetInstance(writer);
                if (cycleUtil.TryGetReferenceId(o, out int id))
                {
                    writer.WriteReference(id);
                    return;
                }
                else
                {
                    writer.WriteId(id);
                }
            }
            foreach (var field in members)
            {
                if (field.HasGetter())
                {
                    var fieldValue = field.Get(o);
                    Write(writer, field, fieldValue);
                }
            }
        }

        private void Write(IDataWriter writer, ObjectMember field, object value)
        {
            string name = field.Name;
            writer.WriteStartNode(name);
            if (value != null)
            {
                var type = value.GetType();
                var customFormatterAttribute = field.Member.GetCustomAttribute<CustomFormatterAttribute>(false);
                var formatter = customFormatterAttribute?.Formatter ?? FormatterFactory.Get(type);

                writer.WriteType(type);
                formatter.Write(writer, value);
            }
            writer.WriteEndNode(name);
        }

        private void WriteTypeString(XmlWriter writer, Type type)
        {
            writer.WriteAttributeString("Type", $"{type}, {type.Assembly.GetName().Name}");
        }

        private Constructor GetDefaultConstructor()
        {
            var publicConstructors = Type.GetConstructors();

            var defaultConstructor = publicConstructors.SingleOrDefault(e => HasAttribute<SerializerConstructorAttribute>(e));
            if (defaultConstructor == null)
            {
                defaultConstructor = Type.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor == null)
                {
                    return null;
                }
                return new Constructor(defaultConstructor, null);
            }

            var attr = defaultConstructor.GetCustomAttribute<SerializerConstructorAttribute>(false);
            return new Constructor(defaultConstructor, attr);
        }

        private ObjectMember[] GetObjectMembers(Type type)
        {
            if (type == typeof(object) || type == null)
                return new ObjectMember[0];

            var publicFields = type.GetFields();
            var privateFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            var publicProperties = type.GetProperties();
            var privateProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

            IEnumerable<FieldInfo> noIgnoreFields;
            IEnumerable<PropertyInfo> noIgnoreProperties;
            IEnumerable<ObjectMember> backingFields = null;
            if (type != Type || HasAttribute<SerializerForceIncludeAttribute>(type))
            {
                noIgnoreFields = privateFields.Concat(publicFields).Where(e => HasAttribute<SerializerIncludeAttribute>(e));
                noIgnoreProperties = privateProperties.Concat(publicProperties).Where(e => HasAttribute<SerializerIncludeAttribute>(e));
            }
            else
            {
                var privateIncludeProperties = privateProperties.Where(e => HasAttribute<SerializerIncludeAttribute>(e));
                var publicBackingProperties = publicProperties.Where(e => HasAttribute<SerializerUseBackingField>(e));
                backingFields = publicBackingProperties
                    .Select(e => new ObjectMember(GetBackingField(e), e.GetCustomAttribute<SerializerIncludeAttribute>(false) ?? new SerializerIncludeAttribute() { Name = e.Name }));
                var allProperties = publicProperties.Except(publicBackingProperties).Concat(privateIncludeProperties);

                noIgnoreProperties = allProperties.Where(e => !HasAttribute<SerializerIgnoreAttribute>(e));

                var privateIncludeFields = privateFields.Where(e => HasAttribute<SerializerIncludeAttribute>(e));
                var allFields = publicFields.Concat(privateIncludeFields);

                noIgnoreFields = allFields.Where(e => !HasAttribute<SerializerIgnoreAttribute>(e));
            }

            return noIgnoreFields.Concat(noIgnoreProperties.Cast<MemberInfo>()).Select(e => new ObjectMember(e, e.GetCustomAttribute<SerializerIncludeAttribute>(false))).Concat(backingFields).Concat(GetObjectMembers(type.BaseType)).ToArray();
        }

        private object[] GetConstructorParameters(Dictionary<string, object> values, SerializerConstructorAttribute constructorAttribute)
        {
            var parameters = new object[constructorAttribute?.Parameters?.Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (values.TryGetValue(constructorAttribute.Parameters[i], out var parameter))
                {
                    parameters[i] = parameter;
                }
            }
            return parameters;
        }

        private FieldInfo GetBackingField(PropertyInfo propertyInfo)
        {
            return propertyInfo.DeclaringType.GetField($"<{propertyInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private bool HasAttribute<T>(MemberInfo member) where T : Attribute
        {
            return member.GetCustomAttribute<T>(false) != null;
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

        private class ObjectMember
        {
            public SerializerIncludeAttribute Attribute { get; set; }
            public MemberInfo Member { get; set; }

            public string Name => Attribute?.Name ?? Member.Name; //Attribute == null ? Member.Name : Attribute.Name ?? Member.Name;

            public ObjectMember(MemberInfo member, SerializerIncludeAttribute attribute)
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