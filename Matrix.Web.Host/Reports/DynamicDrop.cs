using DotLiquid;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Matrix.Web.Host.Reports
{
    public class DynamicDrop : Drop
    {
        private readonly dynamic model;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDrop"/> class.
        /// </summary>
        /// <param name="model">The view model.</param> 
        public DynamicDrop(dynamic model)
        {
            this.model = model;
        }

        public dynamic GetViewModel()
        {
            return model;
        }

        public override bool ContainsKey(object name)
        {
            var bs = base.ContainsKey(name);
            return bs;
        }

        public override object ToLiquid()
        {
            return base.ToLiquid();
        }

        public override object BeforeMethod(string propertyName)
        {
            if (model == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            Type modelType = this.model.GetType();
            object value = null;
            if (modelType.Equals(typeof(ExpandoObject)))
            {
                value = GetExpandoObjectValue(propertyName);
            }
            else
            {
                value = GetPropertyValue(propertyName);
            }
            return value;
        }

        private object GetExpandoObjectValue(string propertyName)
        {
            var dict = this.model as IDictionary<string, object>;
            var value = (!dict.ContainsKey(propertyName)) ?
                null :
                dict[propertyName];
            if (value == null) return null;
            if (value.GetType() == typeof(ExpandoObject))
            {
                return new DynamicDrop(value);
            }
            if (typeof(IEnumerable<object>).IsAssignableFrom(value.GetType()))
            {
                var x = (value as IEnumerable<object>).Select(i => new DynamicDrop(i)).ToArray();
                return x;
            }
            return value;
        }

        private object GetPropertyValue(string propertyName)
        {
            var property = this.model.GetType().GetProperty(propertyName);

            return (property == null) ?
                null :
                property.GetValue(this.model, null);
        }
    }
}