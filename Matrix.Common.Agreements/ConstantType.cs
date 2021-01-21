using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Matrix.Common.Agreements
{
    /// <summary>
    /// константы, возвращаемые счетчиками
    /// если константы нет в этом перечне, задумайся, а нужна ли она
    /// </summary>
    [DataContract]
    public enum ConstantType
    {
        /// <summary>
        /// неизвестная константа
        /// </summary>
        [EnumMember]
        [Parameter("Неизвестный")]
        Unknown,

        /// <summary>
        /// заводской номер
        /// </summary>
        [EnumMember]
        [Parameter("Заводской номер")]
        FactoryNumber,

        /// <summary>
        /// плотность
        /// </summary>
        [EnumMember]
        [Parameter("Плотность")]
        Density,

        /// <summary>
        /// доля азота
        /// </summary>
        [EnumMember]
        [Parameter("Доля азота")]
        Nitrogen,


        /// <summary>
        /// доля углеводорода
        /// </summary>
        [EnumMember]
        [Parameter("Доля углеводорода")]
		Hydrocarbon,

		/// <summary>
		/// доля диоксида углерода
		/// </summary>
		[EnumMember]
		[Parameter("Двуокись углерода CO2")]
		Carbondioxide,
    }
}
