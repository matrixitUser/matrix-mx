using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Matrix.Common.Infrastructure.Pivot
{
	public class RealMappingProperty : PropertyDescriptor
	{
		private readonly string name;
		private object obj;
		public RealMappingProperty(string name, object obj)
			: base(name, null)
		{
			this.name = name;
			this.obj = obj;
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
			return obj;
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
			obj = value;
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
