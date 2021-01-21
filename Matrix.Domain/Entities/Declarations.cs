using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
    public static class OperationTypes
    {
        public static class Survey
        {
            public const string Name = "survey";
            public const string Type = "Type";
            public const string DateStart = "DateStart";
            public const string DateEnd = "DateEnd";
            public const string HolesOnly = "HolesOnly";
        }
        public static class SendBytes
        {
            public const string Name = "SendBytes";
            public const string Bytes = "Bytes";
        }
        public static class SendMaquette
        {
            public const string Name = "SendMaquette";
            public const string Dates = "Dates";

        }
    }
    public static class DataRecordTypes
    {
        public const string MatrixSignalType = "MatrixSignal";
        public const string AbnormalRecordType = "Abnormal";
        public const string HourOccupancyType = "HourCount";
        public const string DayOccupancyType = "DayCount";
        public const string HourRecordType = "Hour";
        public const string DayRecordType = "Day";
        public const string CurrentType = "Current";
        public const string CommunicationLogType = "CommunicationLog";
        public const string LogMessageType = "LogMessage";
        public const string Maquette80020Type = "Maquette80020";
        public const string MailerType = "Mailer";
        public const string ConstantRecordType = "Constant";
        public const string ChangeLogType = "ChangeLog";
        public static class EventType
        {
            public const string Name = "Event";
            public const string Alarm = "Alarm";
            public const string Warning = "Warning";
            public const string Info = "Info";
        }
    }
    public static class DataRecordRequestTypes
    {
        public const string DateStart = "DateStart";
        public const string DateEnd = "DateEnd";
        public const string Last = "Last";
        public static class SurveyData
        {
            public const string DatesOnly = "DatesOnly";
        }
        public static class Event
        {
            public const string LastRecordId = "LastRecordId";
            public const string NotConfirmedOnly = "NotConfirmedOnly";
        }
    }
    public static class TubeTags
    {
        public const string TASK_TAG = "TASK_TAG";
        public const string OCCUPANCY_TYPE_TAG = "DATA_TYPE_TAG";
        public const string OCCUPANCY_TYPE_HOUR = "DATA_TYPE_HOUR";
        public const string OCCUPANCY_TYPE_DAY = "DATA_TYPE_DAY";
        public const string OCCUPANCY_VALUE_TAG = "DATA_VALUE_TAG";
    }
}
