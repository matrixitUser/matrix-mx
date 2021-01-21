using System;
using System.Runtime.Serialization;

namespace Matrix.Common.Agreements
{
    [DataContract]
    public enum AccessModifier : byte
    {
        [EnumMember]
        Deny,
        [EnumMember]
        Read,
        [EnumMember]
        Edit,
    }
    public static class AccessModifierHelper
    {
        public static AccessModifier GetRestrictedModifier(this AccessModifier source, AccessModifier target)
        {
            return (((int)source) <= ((int)target)) ? source : target;
        }
        private static AccessModifier? maxWright;
        /// <summary>
        /// Возвращает максимальные права
        /// </summary>
        public static AccessModifier MaxWright
        {
            get
            {
                if (maxWright == null)
                {
                    var vals = Enum.GetValues(typeof(AccessModifier));
                    var length = vals.Length;
                    maxWright = (AccessModifier)vals.GetValue(length - 1);
                }
                return maxWright.Value;
            }
        }
        private static AccessModifier? minWright;
        /// <summary>
        /// Возвращает минимальные права
        /// </summary>
        public static AccessModifier MinWright
        {
            get
            {
                if (minWright == null)
                {
                    var vals = Enum.GetValues(typeof(AccessModifier));
                    minWright = (AccessModifier)vals.GetValue(0);
                }
                return minWright.Value;
            }
        }
    }
}
