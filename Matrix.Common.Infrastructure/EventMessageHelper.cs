//using System;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//namespace Matrix.Common.Infrastructure
//{
//    public static class EventMessageHelper
//    {
//        public static IEnumerable<Guid> GetObjects(string message)
//        {
//            var result = new List<Guid>();

//            if (string.IsNullOrEmpty(message)) return result;

//            var r = new Regex(@"\[(?<guid>[A-Za-z0-9-]+)\]");
//            MatchCollection matches = r.Matches(message);

//            foreach (Match match in matches)
//            {
//                string value = match.Groups["guid"].Value;
//                Guid g;
//                if (Guid.TryParse(value, out g))
//                {
//                    result.Add(g);
//                }
//            }

//            return result;
//        }

//        public static string GetMessage(string message, ICache cache)
//        {
//            if (string.IsNullOrEmpty(message) || cache == null) return string.Empty;

//            var r = new Regex(@"\[(?<guid>[A-Za-z0-9-]+)\]");
//            //return r.Replace(message, match =>
//            //                       {
//            //                           string value = match.Groups["guid"].Value;
//            //                           Guid g;
//            //                           if (Guid.TryParse(value, out g))
//            //                           {
//            //                               var obj = cache.ById(g);
//            //                               if (obj != null)
//            //                               {
//            //                                   return obj.ToString();
//            //                               }
//            //                           }
//            //                           return value;
//            //                       });
//            return message;

//        }
//    }
//}
