using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Matrix.Common.Maquette
{
    /// <summary>
    /// Макет
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "message")]
    public class Maquette80020
    {
        #region Properties

        /// <summary>
        /// Является обязательным и содержит данные о типе электронного документа. 
        /// Значение атрибута class должно быть равно 80020.
        /// </summary>
        [XmlAttribute(AttributeName = "class")]
        public string Class { get; set; }

        /// <summary>
        /// Является обязательным и содержит данные о версии формата. 
        /// Данный документ определяет версию документа 2.
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Является обязательным и  содержит порядковый номер сообщения.  
        /// (Номера  сообщений  присваиваются  отправителем,  начинаются  с  1  и увеличиваются на 1 с каждым новым сообщением). 
        /// Совпадает с номером документа в  пункте
        /// </summary>
        [XmlAttribute(AttributeName = "number")]
        public int Number { get; set; }

        /// <summary>
        /// Содержит информацию  о времени  создания документа.
        /// </summary>
        [XmlElement(ElementName = "datetime")]
        public DateTimeTag DateTime { get; set; }

        /// <summary>
        /// Описывает  организацию, предоставляющую  информацию
        /// </summary>
        [XmlElement(ElementName = "sender")]
        public Sender Sender { get; set; }

        /// <summary>
        /// Содержит информацию о результатах измерений субъекта ОРЭ
        /// </summary>
        [XmlElement(ElementName = "area")]
        public List<Area> Areas { get; set; }

        /// <summary>
        /// Имя файла
        /// </summary>
        [XmlIgnore]
        public string FileName
        {
            get
            {
                return string.Format("{0}_{1}_{2:yyyyMMdd}_{3}.xml", Class, Sender.Inn, DateTime.Day, Number);
            }
        }

        /// <summary>
        /// Код АСКУЭ
        /// </summary>
        [XmlIgnore]
        public string AmrCode { get; set; }

        #endregion

        #region Constructors

        public Maquette80020()
        {
            Class = "80020";
            Version = 2;
            Areas = new List<Area>();
            DateTime = new DateTimeTag();
            Sender = new Sender();
            AmrCode = "";
        }

        #endregion

        /// <summary>
        /// Загрузка макета из файла (десериализация)
        /// </summary>
        /// <param name="fileName">XML файл макета</param>
        /// <returns>Макет (null - если загрузка не удалась)</returns>
        public static Maquette80020 Load(string fileName)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Maquette80020));
            Maquette80020 result = null;
            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open);
                result = (Maquette80020)ser.Deserialize(fs);
                //result.FileName = Path.GetFileNameWithoutExtension(fileName);

                var fileNameParts = fileName.Split('_', '.');
                if (fileNameParts.Length >= 5)
                {
                    result.AmrCode = fileNameParts[4];
                }
            }
            catch (Exception)
            {
                result = null;
            }
            finally
            {
                if (fs != null) fs.Close();
            }
            return result;
        }

        public string GetFileName(string amrCode)
        {
            return string.Format("{0}_{1}_{2}_{3}_{4}.xml",
                    Class, Sender.Inn, DateTime.Day, Number, amrCode);
        }

        /// <summary>
        /// Загрузка макета из файла (десериализация)
        /// </summary>
        /// <param name="fileName">XML файл макета</param>
        /// <returns>Макет (null - если загрузка не удалась)</returns>
        public static Maquette80020 Load(Stream stream)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Maquette80020));
            Maquette80020 result = null;
            try
            {
                result = (Maquette80020)ser.Deserialize(stream);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (stream != null) stream.Close();
            }
            return result;
        }

        public bool Save(string directoryName)
        {
            return Save(directoryName, AmrCode);
        }

        public string Save()
        {
            string result = "empty";

            XmlSerializer serializer = new XmlSerializer(typeof(Maquette80020));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = false; // не подавлять xml заголовок
            settings.Encoding = Encoding.UTF8; // кодировка
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true; // добавлять отступы
            settings.IndentChars = "    "; // символы отступа

            MemoryStream s = new MemoryStream();

            using (XmlWriter xmlWriter = XmlWriter.Create(s, settings))
            {
                try
                {
                    serializer.Serialize(xmlWriter, this);
                    byte[] arr = s.ToArray();
                    result = Encoding.UTF8.GetString(arr);
                }
                catch (Exception ex)
                {
                    result = "";
                }
                finally
                {
                }
            }
            
            return result;
        }

        public string Save1251()
        {
            string result = "empty";

            XmlSerializer serializer = new XmlSerializer(typeof(Maquette80020));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = false; // не подавлять xml заголовок
            settings.Encoding = Encoding.GetEncoding(1251); // кодировка
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true; // добавлять отступы
            settings.IndentChars = "    "; // символы отступа

            MemoryStream s = new MemoryStream();

            using (XmlWriter xmlWriter = XmlWriter.Create(s, settings))
            {
                try
                {
                    serializer.Serialize(xmlWriter, this);
                    byte[] arr = s.ToArray();
                    result = Encoding.GetEncoding(1251).GetString(arr);
                }
                catch (Exception)
                {
                    result = "";
                }
                finally
                {
                }
            }

            return result;
        }

        public byte[] GetBytes()
        {
            byte[] result = null;

            XmlSerializer serializer = new XmlSerializer(typeof(Maquette80020));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.GetEncoding(1251); // кодировка

            MemoryStream s = new MemoryStream();

            try
            {
                serializer.Serialize(s, this);
                result = s.ToArray();
            }
            catch (Exception)
            {
                result = null;
            }
            finally
            {
            }
            return result;
        }

        public bool SaveAs(string fileName)
        {
            bool result = false;

            XmlSerializer serializer = new XmlSerializer(typeof(Maquette80020));
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Create);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                XmlWriter xr = XmlWriter.Create(fs, settings);


                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(xr, this, ns);
                //serializer.Serialize(fs, this);
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }
            finally
            {
                if (fs != null) fs.Close();
            }
            return result;
        }

        public bool Save(string directoryName, string amrCode)
        {
            bool result = false;

            //FileName = string.Format("{0}_{1}_{2}_{3}_{4}.xml",
            //        Class, Sender.Inn, DateTime.Day, Number, amrCode);

            string fullFileName = directoryName + Path.DirectorySeparatorChar + FileName;

            XmlSerializer serializer = new XmlSerializer(typeof(Maquette80020));
            FileStream fs = null;

            try
            {
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                fs = new FileStream(fullFileName, FileMode.Create);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                XmlWriter xr = XmlWriter.Create(fs, settings);


                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(xr, this, ns);
                //serializer.Serialize(fs, this);
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            finally
            {
                if (fs != null) fs.Close();
            }
            return result;
        }

        public bool HasNonCommercials()
        {
            foreach (var area in Areas)
            {
                foreach (var point in area.MeasuringPoints)
                {
                    foreach (var channel in point.MeasuringChannels)
                    {
                        foreach (var period in channel.Periods)
                        {
                            if (period.Value.Status != 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
