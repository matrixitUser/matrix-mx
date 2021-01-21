using System;
using System.Linq;

namespace Matrix.Common.Agreements
{
    public class ExDisplayAttribute : Attribute
    {
        public string MainName { get; set; }

        /// <summary>
        /// Родительный падеж
        /// </summary>
        public string GenitiveCase { get; set; }
        /// <summary>
        /// Дательный падеж
        /// </summary>
        public string DativeCase { get; set; }
        /// <summary>
        /// Винительный падеж
        /// </summary>
        public string AccusativeCase { get; set; }
        /// <summary>
        /// Творительный падеж
        /// </summary>
        public string InstrumentalCase { get; set; }
        /// <summary>
        /// Предложный падеж
        /// </summary>
        public string PrepositionalCase { get; set; }
    }

    public static class DisplayHelper
    {
        public static string GetDisplayName(object obj)
        {
            return GetDisplayName(obj.GetType());
        }

        public static string GetDisplayName(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(ExDisplayAttribute), true).OfType<ExDisplayAttribute>();
            foreach (var exDisplayAttribute in attributes)
            {
                if (!string.IsNullOrEmpty(exDisplayAttribute.MainName))
                    return exDisplayAttribute.MainName;
            }
            return string.Empty;
        }
        public static string GetAccusativeCase(object obj)
        {
            return GetAccusativeCase(obj.GetType());
        }
        public static string GetAccusativeCase(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(ExDisplayAttribute), true).OfType<ExDisplayAttribute>();
            foreach (var exDisplayAttribute in attributes)
            {
                if (!string.IsNullOrEmpty(exDisplayAttribute.AccusativeCase))
                    return exDisplayAttribute.AccusativeCase;
            }
            return string.Empty;
        }
    }
}
