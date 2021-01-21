using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Matrix.Common.Infrastructure.Pivot
{
    public class DynamicProperty : PropertyDescriptor
    {
        private readonly string name;
        public DynamicProperty(string name)
            : base(name, null)
        {
            this.name = name;
        }

        private readonly List<Attribute> attributes = new List<Attribute>();
        public void AddAttribute(Attribute attribute)
        {
            attributes.Add(attribute);
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(attributes.ToArray());
            }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return typeof(DynamicObject); }
        }

        public override object GetValue(object component)
        {
            var obj = component as DynamicObject;
            if (!obj.ContainsKey(name)) return null;
            return obj[name];
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get
            {
                return typeof(object);
            }
        }

        public override void ResetValue(object component)
        {

        }

        public override void SetValue(object component, object value)
        {
            var obj = component as DynamicObject;
            obj[name] = value;
            RaisePropertyChanged(name);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
