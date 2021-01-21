//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SPG761.Protocol
//{
//    static class SugarExtensions
//    {
//        public static void SetFNC(this Message message, FunctionCode FNC)
//        {
//            message.Head.FNC = FNC;
//        }

//        public static void AddDate(this Message message, DateTime date)
//        {
//            var category = new Category();
//            category.Fields.Add(new Field(date.ToString("dd")));
//            category.Fields.Add(new Field(date.ToString("MM")));
//            category.Fields.Add(new Field(date.ToString("yy")));
//            category.Fields.Add(new Field(date.ToString("HH")));
//            category.Fields.Add(new Field(date.ToString("mm")));
//            category.Fields.Add(new Field(date.ToString("ss")));
//            message.Body.Categories.Add(category);
//        }

//        public static void AddParameter(this Message message, int channel, int number)
//        {
//            message.AddParameter(channel.ToString(), number.ToString());
//        }
//        public static void AddParameter(this Message message, string channel, string number)
//        {
//            var category = new Category();
//            category.Fields.Add(new Field(channel));
//            category.Fields.Add(new Field(number));
//            message.Body.Categories.Add(category);
//        }

//        public static void AddArray(this Message message, int channel, int number, int startIndex, int count)
//        {
//            message.AddArray(channel.ToString(), number.ToString(), startIndex, count);
//        }

//        public static void AddArray(this Message message, string channel, string number, int startIndex, int count)
//        {
//            var category = new Category();
//            category.Fields.Add(new Field(channel));
//            category.Fields.Add(new Field(number));
//            category.Fields.Add(new Field(startIndex.ToString()));
//            category.Fields.Add(new Field(count.ToString()));
//            message.Body.Categories.Add(category);
//        }

//        public static DateTime AsDate(this Category category)
//        {
//            DateTime date = DateTime.MinValue;
//            if (category.Fields.Count >= 6)
//            {
//                int day = 0; int.TryParse(category.Fields[0].Text, out day);
//                int month = 0; int.TryParse(category.Fields[1].Text, out month);
//                int year = 0; int.TryParse(category.Fields[2].Text, out year);
//                int hour = 0; int.TryParse(category.Fields[3].Text, out hour);
//                int minute = 0; int.TryParse(category.Fields[4].Text, out minute);
//                int second = 0; int.TryParse(category.Fields[5].Text, out second);
//                date = new DateTime(year + 2000, month, day, hour, minute, second);
//            }
//            return date;
//        }

//        public static double AsDouble(this Category category)
//        {
//            double value = 0.0;

//            double.TryParse(category.Fields[0].Text.Replace(".", ","), out value);

//            return value;
//        }
//    }
//}
