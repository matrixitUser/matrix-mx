//using System;
//using System.Collections.Generic;
//using System.Dynamic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Common
//{
//    public class DynamicRecord : DynamicObject
//    {
//        private readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();

//        public override bool TryGetMember(GetMemberBinder binder, out object result)
//        {
//            if (dictionary.ContainsKey(binder.Name))
//            {
//                result = dictionary[binder.Name];
//                return true;
//            }
//            result = null;
//            return true;
//        }

//        public override bool TrySetMember(SetMemberBinder binder, object value)
//        {
//            if (dictionary.ContainsKey(binder.Name))
//            {
//                dictionary[binder.Name] = value;
//                return true;
//            }
//            dictionary.Add(binder.Name, value);
//            return true;
//        }
//    }
//}
