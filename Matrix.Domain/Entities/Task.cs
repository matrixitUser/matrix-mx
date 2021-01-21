using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
    [DataContract]
    public class Task : Entity //: AggregationRoot
    {
        public const string HourlySurveyTask = "HourlySurveyTask";
        public const string DailySurveyTask = "DailySurveyTask";
        public const string CurrentSurveyTask = "CurrentSurveyTask";
        public const string MaquetteTask = "MaquetteTask";
        public const string AbnormalSurveyTask = "AbnormalSurveyTask";
        public const string HourlyOccupancyTask = "HourlyOccupancyTask";
        public const string DailyOccupancyTask = "DailyOccupancyTask";     

        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String Cron { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public bool IsEnabled { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
