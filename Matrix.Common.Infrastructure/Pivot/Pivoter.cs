using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Matrix.Common.Infrastructure.Pivot
{
    public static class Pivoter
    {
        public static IEnumerable<DynamicObject> Pivot<TSource, TFixedGroup, TProperty, TValue>(IEnumerable<TSource> data,
            Func<TSource, TFixedGroup> fixedFields,
            Func<TSource, TProperty> propertyField,
            Func<TSource, TValue> valueField, params string[] aliases)
        {
            if (data == null) return null;

            Func<int, string, string> GetAlias = (index, orDefault) =>
            {
                if (aliases == null || aliases.Length <= index)
                {
                    if (string.IsNullOrEmpty(orDefault))
                        return string.Format("Param{0}", index);
                    return orDefault;
                }
                return aliases[index];
            };

            Func<Type, bool> CheckIsSingle = type => type.IsPrimitive || type == typeof(DateTime) || type == typeof(Guid);

            var result = new List<DynamicObject>();
            foreach (var rowData in data.GroupBy(fixedFields))
            {
                var obj = new DynamicObject();
                foreach (var record in rowData)
                {
                    var fixedGroup = fixedFields(record);
                    if (CheckIsSingle(fixedGroup.GetType()))
                    {
                        var name = GetAlias(0, null);
                        if (!obj.ContainsKey(name))
                            obj.Add(name, fixedGroup);
                    }
                    else
                    {
                        int index = 0;
                        foreach (var fixedPoperty in fixedGroup.GetType().GetProperties())
                        {
                            var name = GetAlias(index, fixedPoperty.Name);
                            if (obj.ContainsKey(name)) continue;
                            try
                            {
                                var val = fixedPoperty.GetValue(fixedGroup, null);
                                obj.Add(name, val);
                                index++;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    var propName = propertyField(record).ToString();
                    var propValue = valueField(record);
                    if (obj.ContainsKey(propName)) continue;
                    if (propValue is Nullable<double>)
                    {
                        obj.Add(propName, (propValue as Nullable<double>).Value);
                    }
                    else
                    {
                        obj.Add(propName, propValue);
                    }
                }

                result.Add(obj);
            }
            return result;
        }


        public static IEnumerable<DynamicObject> Pivot<TSource>(IEnumerable<TSource> data,
            string pointField,
            string propertyField,
            IEnumerable<string> valueFields)
        {
            if (data == null) return null;

            var result = new List<DynamicObject>();
            var point = typeof(TSource).GetProperty(pointField);
            var property = typeof(TSource).GetProperty(propertyField);
            var values = new List<PropertyInfo>();
            foreach (var valueField in valueFields)
            {
                var value = typeof(TSource).GetProperty(valueField);
                values.Add(value);
            }

            foreach (var rowData in data.GroupBy(d => point.GetValue(d, null)))
            {
                var obj = new DynamicObject();
                foreach (var record in rowData)
                {
                    var fixedGroup = point.GetValue(record, null);

                    var name = pointField;
                    if (!obj.ContainsKey(name))
                        obj.Add(name, fixedGroup);

                    var propName = property.GetValue(record, null).ToString();
                    object propValue = null;
                    if (values.Count() > 1)
                    {
                        propValue = new DynamicObject();
                        foreach (var value in values)
                        {
                            (propValue as DynamicObject).Add(value.Name, value.GetValue(record, null));
                        }
                    }
                    else
                    {
                        var value = values.FirstOrDefault();
                        propValue = value.GetValue(record, null);
                    }
                    if (obj.ContainsKey(propName)) continue;
                    obj.Add(propName, propValue);
                }

                result.Add(obj);
            }
            return result;
        }
    }
}
