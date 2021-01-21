using System;
using System.Runtime.Serialization;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Matrix.Domain.Entities
{
    /// <summary>
    /// Параметр трубы
    /// </summary>
    [DataContract]
    [Serializable]
    public class TubeParameter : Entity// AggregationRoot
    {       

        [DataMember]        
        public Guid TubeId { get; set; }
        [DataMember]
        public string SystemParameterId { get; set; }
        [DataMember]
        public string CalculationTypeId { get; set; }
        [DataMember]
        public string MeasuringUnitId { get; set; }
        
        public override string ToString()
        {
            var taggedName = this.GetStringTag(PARAMETER_TAG) ?? "";
            var taggedGroup = this.GetStringTag(GROUP_TAG) ?? "";

            string displayName = string.Format("{0}{1}", taggedName, taggedGroup);
            if (string.IsNullOrEmpty(displayName)) displayName = SystemParameterId;
            return displayName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TubeParameter)) return false;
            var other = obj as TubeParameter;
            return other.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        //public string GetField()
        //{
        //    string result;

        //    Tag displayTag = Tags.FirstOrDefault(t => t.Name == PARAMETER_TAG);
        //    Tag groupTag = Tags.FirstOrDefault(t => t.Name == GROUP_TAG);


        //    if (displayTag != null)
        //    {
        //        result = displayTag.Value;
        //        if (groupTag != null)
        //            result += groupTag.Value;
        //    }
        //    else
        //    {
        //        result = string.Format("{0}{1}", SystemParameterId, CalculationType);
        //    }
        //    return result;
        //}

        /// <summary>
        /// признак того, что параметр вычислимый
        /// </summary>
        [DataMember]
        public bool IsVirtual { get; set; }

        #region теги
        /// <summary>
        /// тег-параметр
        /// </summary>
        public const string PARAMETER_TAG = "PARAMETER_TAG";

        public static IEnumerable<string> GetParameters()
        {
            var parameters = new List<string>();
            foreach (FieldInfo field in typeof(TubeParameter).GetFields().Where(f => f.Name.EndsWith("_PARAMETER")))
            {
                parameters.Add(field.GetRawConstantValue().ToString());
            }
            return parameters;
        }
        public static IEnumerable<string> GetGroups()
        {
            var parameters = new List<string>();
            foreach (FieldInfo field in typeof(TubeParameter).GetFields().Where(f => f.Name.EndsWith("_PARAMETER")))
            {
                parameters.Add(field.GetRawConstantValue().ToString());
            }
            return parameters;
        }

        #region значения тега PARAMETER_TAG
        /// <summary>
        /// давление
        /// </summary>
        public const string PRESSURE_PARAMETER = "P";

        /// <summary>
        /// температура
        /// </summary>
        public const string TEMPERATURE_PARAMETER = "T";

        /// <summary>
        /// объемный расход при р.у.
        /// </summary>
        public const string VOLUME_CONSUMPTION_WORK_PARAMETER = "VolumeConsumptionWork";

        /// <summary>
        /// объемный расход при н.у.
        /// </summary>
        public const string VOLUME_CONSUMPTION_NORMAL_PARAMETER = "VolumeConsumptionNormal";

        /// <summary>
        /// тепло
        /// </summary>
        public const string HEAT_PARAMETER = "Heat";

        /// <summary>
        /// масса
        /// </summary>
        public const string MASS_PARAMETER = "M";

        /// <summary>
        /// объем
        /// </summary>
        public const string VOLUME_PARAMETER = "V";

        /// <summary>
        /// время наработки
        /// </summary>
        public const string TIME_WORK_PARAMETER = "TimeWork";

        /// <summary>
        /// наличие нештатной ситуации
        /// </summary>
        public const string EMERGENCY_PARAMETER = "Emergency";
        /// <summary>
        /// наличие нештатной ситуации
        /// </summary>
        public const string CURRENT_DATETIME = "CurrentDateTime";
        #endregion

        /// <summary>
        /// признак принадлежности к группе
        /// </summary>
        public const string GROUP_TAG = "GROUP_TAG";
        #region значения тега GROUP_TAG
        /// <summary>
        /// горячее водоснабжение, для тепловых счетчиков
        /// </summary>
        public const string GROUP_GVS = "ГВС";
        #endregion

        public const string MAX_VALUE_TAG = "MaxValue";
        public const string MIN_VALUE_TAG = "MinValue";
        public const string NIGHT_MAX_VALUE_TAG = "NightMaxValue";
        public const string NIGHT_MIN_VALUE_TAG = "NightMinValue";
        public const string NIGHT_START_HOUR_TAG = "NightStartHour";
        public const string NIGHT_END_HOUR_TAG = "NightEngHour";

        public const string IS_CURRENT_TAG = "текущий";
        #endregion

        public string ToLongString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Параметр (");
            sb.AppendFormat("имя={0};", SystemParameterId);
            sb.AppendFormat("единица изм.={0};", MeasuringUnitId);
            sb.AppendFormat("тип={0};", CalculationTypeId);
            foreach (var tag in Tags)
            {
                sb.AppendFormat("{0}={1};", tag.Name, tag.Value);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
