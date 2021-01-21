using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    /// <summary>
    /// Управление задвижками
    /// </summary>
    public partial class Driver
    {
        /// <summary>
        /// Функция для управления задвижками
        /// формирование байтов для отправки, принятие данных и парсинг
        /// </summary>
        /// <param name="str">строка с данными, которую нужно распарсить</param>
        /// <param name="flashver">версия контроллера</param>
        /// <param name="objectId">id объекта(tube)</param>
        /// <returns></returns>
        public dynamic valveControlSetConfig(string str, dynamic flashver, string objectId)
        {
            if (flashver == null)
            {
                flashver = GetFlashVer(); //получение версии
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;

            // формирование байтов для отправки
            List<byte> listByte = listByteValveControlSetConfig(str); 
            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray(); //отправляем id объекта
            listByte.AddRange(byteObjectId);

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();

            dynamic valveControl = new ExpandoObject();
            if (ver == 6) //может придется менять версию
            {
                var result = Send(MakeValveControlRequest((Int32)0x0100, (ushort)listByte.Count, listByte));
                if (!result.success)
                {
                    log(string.Format("valveControlSetConfig не введён: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }
                //если пришли байты не на ту функцию, то не принимаем
                if (result.Function != 0x49) // нужно написать какая функция  
                {
                    log(string.Format("Получен ответ {0} не на 73 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                UInt32 uInt32Time = (UInt32)(result.Body[8] << 24) | (UInt32)(result.Body[7] << 16) | (UInt32)(result.Body[6] << 8) | result.Body[5];
                DateTime dtContollers = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(uInt32Time);
                log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);

                //сравнение времени контроллера и времени на сервере, производим корректировку, если разница большая
                DateTime now = DateTime.Now;
                var diffSecond = ((dtContollers > now) ? (dtContollers - now).TotalSeconds : (now - dtContollers).TotalSeconds);
                var diffDay = ((dtContollers > now) ? (dtContollers - now).TotalDays : (now - dtContollers).TotalDays);
                if (diffSecond > TimeSecondForSet)
                {
                    CorrectTime(flashver); // корректировка времени
                    if (diffDay > TimeDayForSet)
                    {
                        log($"{msgSetTime}  разница: {diffDay} дней", level: 1);
                        return valveControlSetConfig("", flashver, objectId); // простой опрос после корректировки времени
                    }

                }
                byte heatingSupply = (byte)result.Body[0];  //Отопление подача
                byte heatingReturn = (byte)result.Body[1];  //Отопление обратка
                byte hwsSupply = (byte)result.Body[2];      //ГВС подача
                byte hwsReturn = (byte)result.Body[3];      //ГВС обратка
                byte cws = (byte)result.Body[4];            //ХВС

                DateTime date = DateTime.Now;
                log(string.Format("Отопление подача: {0}; Отопление обратка: {1}; ГВС подача: {2}; ГВС обратка: {3}; ХВС: {4}", ParseValveValueToString(heatingSupply), ParseValveValueToString(heatingReturn), ParseValveValueToString(hwsSupply), ParseValveValueToString(hwsReturn), ParseValveValueToString(cws)), level: 1);
                //запись в таблицу Current
                records.Add(MakeCurrentRecord("heatingSupply", heatingSupply, "", dtContollers, date));
                records.Add(MakeCurrentRecord("heatingReturn", heatingReturn, "", dtContollers, date));
                records.Add(MakeCurrentRecord("hwsSupply", hwsSupply, "", dtContollers, date));
                records.Add(MakeCurrentRecord("hwsReturn", hwsReturn, "", dtContollers, date));
                records.Add(MakeCurrentRecord("cws", cws, "", dtContollers, date));
                string controllerData = $"heatingSupply:{heatingSupply};heatingReturn:{heatingReturn};hwsSupply:{hwsSupply};hwsReturn:{hwsReturn};cws:{cws};dt:{dtContollers}T|";
                
                valveControl.controllerData = controllerData;
                //setModbusControl(valveControl); // для записи в таблицу rowCache
                
            }
            
            current.records = records;
            return current;
        }

        public List<byte> listByteValveControlSetConfig(string str)
        {
            string[] arrValve;
            if (str.Contains("|")) arrValve = str.Split('|');
            else
            {
                arrValve = new string[1];
                arrValve[0] = str;
            } 
            List<byte> listSetConfig = new List<byte>();
            int valveIndex;
            for(int i = 0; i < 5; i++) // заполнение всех значений в 0xFF
            {
                listSetConfig.Add(0xFF);
            }
            if (arrValve[0].Contains("all"))
            {
                // каждая труба имеет расположение(индекс) в списке listSetConfig
                // если для всех труб пришла команда открытия/закрытия, то меняем значение элемента в списке listSetConfig
                for (int i = 0; i < 5; i++) // заполнение всех значений в 0xFF
                {
                    listSetConfig[i] = ParseValveValueToBytes(arrValve[0].Split(':')[1]);
                }
            }
            else
            {
                // каждая труба имеет расположение(индекс) в списке listSetConfig
                // если для определенной трубы пришла команда открытия/закрытия, то меняем значение элемента в списке listSetConfig
                for (int i = 0; i < arrValve.Length; i++)
                {
                    if (arrValve[i].Contains(':'))
                    {
                        valveIndex = ParseValveNameInIndex(arrValve[i].Split(':')[0]);
                        if (valveIndex >= 0)
                        {
                            listSetConfig[valveIndex] = ParseValveValueToBytes(arrValve[i].Split(':')[1]);
                        }
                    }
                }
            }
           
            return listSetConfig;
        }

        //закрыта === 0; открыта === 1
        public string ParseValveValueToString(byte byteValve)
        {
            switch (byteValve)
            {
                case 0x00:
                    return "закрыто";
                case 0x01:
                    return "открыто";
            }
            return "неизвестна";
        }
        //закрыть === 0; открыть === 1
        public byte ParseValveValueToBytes(string strValve)
        {
            switch (strValve.ToLower())
            {
                case "isopen": //если открыта, то закрыть
                case "toclose": 
                case "открыто":
                case "открыта":
                case "закрыть":
                    return 0x00;
                case "isclose":
                case "toopen":
                case "закрыто":
                case "закрыта":
                case "открыть":
                    return 0x01;
            }
            return 0xFF;
        }
        //функция распределяющая каждой трубе индекс для списка
        public int ParseValveNameInIndex(string name)
        {
            switch (name)
            {
                case "heatingSupply": //
                    return 0;
                case "heatingReturn":
                    return 1;
                case "hwsSupply":
                    return 2;
                case "hwsReturn":
                    return 3;
                case "cws":
                    return 4;
            }
            return -1;
        }
        
        
    }
}
