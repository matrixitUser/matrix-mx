using System.Collections.Generic;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
    public class DeliveryGroup
    {
        public DeliveryGroup()
        {
            AlgorithmVersion = 1;
        }

        [XmlAttribute(AttributeName="code")]
        public string Code { get; set; }

        /// <summary>
        /// Номер версии алгоритма расчета сальдо перетоков (ГТП генерации)
        /// является обязательным, отсутствие атрибута эквивалентно записи algorithmversion=1
        /// </summary>
        [XmlAttribute(AttributeName="algorithmversion")]
        public int AlgorithmVersion { get; set; }

        /// <summary>
        /// Наименование группы точек поставки
        /// </summary>
        [XmlAttribute(AttributeName="name")]
        public string Name { get; set; }

        /// <summary>
        /// Список периодов измерения
        /// </summary>
        [XmlElement(ElementName = "period")]
        public List<Period> Periods { get; set; }
    }
}
