using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Matrix.Common.Infrastructure.Pivot
{
    public static class DynamicObjectExtensions
    {
        /// <summary>
        /// выравнивает коллекцию динамических объектов,
        /// добавляет недостающие свойства для каждого элемента
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IEnumerable<DynamicObject> Align(this IEnumerable<DynamicObject> collection)
        {
            if (collection == null || !collection.Any()) return collection;
            var allProperties = collection.SelectMany(c => c.Keys).Distinct();                         
            foreach (var item in collection)
            {
                foreach (var property in allProperties)
                {
                    if (!item.ContainsKey(property))
                    {
                        item.Add(property, null);
                    }
                }
            }
            return collection;
        }
    }

    public class DynamicObject : Dictionary<string, object>, ICustomTypeDescriptor
    {
        //private static List<string> properties = new List<string>();
        //public static void ResetProperties()
        //{
        //    properties.Clear();
        //}
        //private static void AddProperty(string property)
        //{
        //    lock (properties)
        //    {
        //        if (!properties.Any(p => p == property))
        //        {
        //            properties.Add(property);
        //        }
        //    }
        //}

        public static string RestrictedCharsReplace(string source)
        {
            return source.Replace(" ", "").
                Replace("(", "").
                Replace(")", "").
                Replace(".", "");
        }

        public void AddPropertyAttribute(string property, Attribute attribute)
        {
            var normalProperty = RestrictedCharsReplace(property);
            var dp = properties.FirstOrDefault(p => p.Name == normalProperty);
            if (dp != null)
            {
                dp.AddAttribute(attribute);
            }
        }

        public new void Add(string key, object value)
        {
            var allowedKey = RestrictedCharsReplace(key);
            if (ContainsKey(allowedKey))
            {
                base[allowedKey] = value;
            }
            else
            {
                base.Add(allowedKey, value);
                properties.Add(new DynamicProperty(allowedKey));
            }
            //AddProperty(allowedKey);
        }

        public DynamicObject()
        {
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return null;
        }

        public TypeConverter GetConverter()
        {
            return null;
        }

        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        public object GetEditor(Type editorBaseType)
        {
            return null;
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        private readonly List<DynamicProperty> properties = new List<DynamicProperty>();
        public PropertyDescriptorCollection GetProperties()
        {
            return new PropertyDescriptorCollection(properties.ToArray());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }
}
