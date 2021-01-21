using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    class Glossary
    {
        /// <summary>
        /// давление газа
        /// </summary>
        public static string P(byte ch)
        {
            return string.Format("P{0}(давление газа)", ch);
        }

        /// <summary>
        /// время счета
        /// </summary>
        public static string ВНР { get { return "ВНР(время счета)"; } }

        /// <summary>
        /// Сборка НС
        /// </summary>
        public static string HC { get { return "НС(нештатные ситуации)"; } }

        /// <summary>
        /// средняя температура газа
        /// </summary>
        public static string T(byte ch)
        {
            return string.Format("T{0}(температура газа)", ch);
        }
        //public static string T { get { return "T{0}(температура газа)"; } }

        /// <summary>
        /// Рабочий расход
        /// </summary>
        //public static string Vp { get { return "Vр{0}(интегр. объем при р.у.)"; } }
        public static string Vp(byte ch)
        {
            return string.Format("Vр{0}(рабочий объем)", ch);
        }

        /// <summary>
        /// Приведенный объем по каналу
        /// </summary>
        public static string V(byte ch)
        {
            return string.Format("V{0}(приведенный объем)", ch);
        }

        /// <summary>
        /// Стандартный объем
        /// </summary>
        public static string Vс { get { return string.Format("V(стандартный объем)"); } }


        /// <summary>
        /// Допускаемый перепад давления по каналу
        /// </summary>
        public static string dPd(byte ch)
        {
            return string.Format("dP{0}д(перепад давления допускаемый)", ch);
        }

        /// <summary>
        /// Среднее значение коэффициента сжимаемости по каналу
        /// </summary>
        public static string Ksj(byte ch)
        {
            return string.Format("Ксж{0}(коэф. сжимаемости)", ch);
        }

        /// <summary>
        /// Среднее значение коэффициента приведения по каналу
        /// </summary>
        public static string Kpr(byte ch)
        {
            return string.Format("Кпр{0}(коэф. приведения)", ch);
        }

        /// <summary>
        /// Перепад давления
        /// </summary>
        public static string dP(byte ch)
        {
            return string.Format("dP{0}(перепад давления)", ch);
        }

        /// <summary>
        /// барометрическое давление
        /// </summary>
        public static string Pb { get { return "Pб(барометрическое давление)"; } }


        /// <summary>
        /// Приращение рабочего объема с начала часа
        /// </summary>
        public static string Vрч(byte ch)
        {
            return string.Format("Vр{0}ч(приращение рабочего объема)", ch);
        }

        /// <summary>
        /// Приращение стандартного объема с начала часа
        /// </summary>
        public static string Vч(byte ch)
        {
            return string.Format("V{0}ч(приращение стандартного объема)", ch);
        }
    }
}
