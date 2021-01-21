using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.Mercury206
{
    public class Driver : BaseDriver
    {
        public override SurveyResultData ReadCurrentValues()
        {
            var records = new List<Data>();

            records.AddRange(ReadActiveEnergy());
            records.AddRange(ReadReactiveEnergy());
            records.AddRange(ReadUIP());

            return new SurveyResultData { Records = records };
        }
        private IEnumerable<Data> ReadActiveEnergy()
        {
            var dt = GetAnswer(0x27);
            if (dt == null) return new List<Data>();
            var result = new double[dt.Length / 4];

            if (result.Length > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    result[i] = BitConverter.ToUInt32(dt, i * 4) * 0.1;
                }
            }
            int j = 0;
            return result.Select(r => new Data("Активная мощность по тарифу " + (++j), MeasuringUnitType.Wt, DateTime.Now, r));
        }
        private IEnumerable<Data> ReadReactiveEnergy()
        {
            var dt = GetAnswer(0x85);
            if (dt == null) return new List<Data>();
            var result = new double[dt.Length / 4];

            if (result.Length > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    result[i] = BitConverter.ToUInt32(dt, i * 4) * 0.1;
                }
            }
            int j = 0;
            return result.Select(r => new Data("Реактивная мощность по тарифу " + (++j), MeasuringUnitType.Wt, DateTime.Now, r));
        }
        private IEnumerable<Data> ReadUIP()
        {
            var dt = GetAnswer(0x85);
            var result = new List<Data>();
            if (dt == null || dt.Length != 7) return result;

            result.Add(new Data("U", MeasuringUnitType.V, DateTime.Now, ConvertFromBcd(dt, 0, 2) / 10));
            result.Add(new Data("I", MeasuringUnitType.A, DateTime.Now, ConvertFromBcd(dt, 2, 2) / 10));
            result.Add(new Data("P", MeasuringUnitType.Wt, DateTime.Now, ConvertFromBcd(dt, 4, 7) / 10));

            return result;
        }

        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
        {
            if (dates == null)
            {
                return new SurveyResultData { State = SurveyResultState.InvalidIncomingParameters };
            }
            var records = new List<Data>();
            DateTime currentTime = ReadCurrentTime();
            var blocks = CreateBlocks(dates);
            foreach (var block in blocks)
            {
                var rr = ReadHalfHourBlock(block);
                records.AddRange(rr);
            }

            return new SurveyResultData { Records = records, State = SurveyResultState.Success };
        }
        #region low
        private byte[] SendMessageToDevice(byte[] request)
        {
            byte[] response = null;
            bool success = false;
            int attemtingCount = 0;

            while (!success && attemtingCount < 5)
            {
                attemtingCount++;
                //OnSendMessage(string.Format("отправка запроса {0}, попытка {1}", request, attemtingCount));

                isDataReceived = false;
                receivedBuffer = null;
                //OnSendMessage(string.Format("отправлено: [{0}]", string.Join(",", bytes.Select(b => b.ToString("X2")))));                
                RaiseDataSended(request);
                Wait(3000);

                if (isDataReceived)
                {
                    var x = receivedBuffer;
                    response = receivedBuffer;
                    //OnSendMessage(string.Format("получено: [{0}]", string.Join(",", response.Select(b => b.ToString("X2")))));
                    success = true;
                }
            }
            return response;
        }
        private byte[] GetAnswer(byte cmd, byte[] data = null)
        {
            var rqst = CreatePacket(cmd, data);
            var rowData = SendMessageToDevice(rqst);
            return GetResponseData(rowData, cmd);
        }
        private byte[] CreatePacket(byte cmd, byte[] data = null)
        {
            var result = new List<byte>();

            result.AddRange(GetNaPacket());
            result.Add(cmd);
            if (data != null)
                result.AddRange(data);
            var crc = new Crc16Modbus();
            result.AddRange(crc.Calculate(result.ToArray(), 0, result.Count).CrcData);

            return result.ToArray();
        }
        private byte[] GetResponseData(byte[] data, byte cmd)
        {
            if (data == null || data.Length < 3) return null;
            var na = GetNaPacket();

            if (na[0] != data[0] || na[1] != data[1] || na[2] != data[2] || na[3] != data[3] || cmd != data[4])
            {
                return null;
            }

            var crc = new Crc16Modbus();
            var crc16 = crc.Calculate(data, 0, data.Length - 2);
            if (crc16.CrcData.Length != 2) return null;
            if (crc16.CrcData[0] != data[data.Length - 2] || crc16.CrcData[1] != data[data.Length - 1]) return null;

            return data.Take(data.Length - 2).Skip(5).ToArray();
        }
        private byte[] GetNaPacket()
        {
            UInt32 integ;
            if (!UInt32.TryParse(Password, out integ))
            {
                return new byte[0];
            }
            var result = BitConverter.GetBytes(integ);
            return result.Reverse().ToArray();
        }
        #endregion
        #region Convert
        private int ConvertFromBcd(byte bcd)
        {
            return ConvertFromBcd(new byte[] { bcd }, 0, 1);
        }
        private int ConvertFromBcd(byte[] bcd, int startIndex, int length)
        {
            if (bcd == null || startIndex < 0 || length <= 0 || startIndex >= bcd.Length || startIndex + length > bcd.Length)
                return 0;

            string str = string.Empty;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                str += bcd[i].ToString("X");
            }
            int result = 0;
            if (int.TryParse(str, out result))
            {
                return result;
            }
            return 0;
        }
        private byte ConvertToBcd(int value)
        {
            var valStr = value.ToString();
            return Convert.ToByte(valStr, 16);
            //byte result;
            //byte.TryParse("0x" + valStr, out result);
            //return result;
        }
        #endregion
        private DateTime ReadCurrentTime()
        {
            var dt = GetAnswer(0x21);
            var errorMessage = "Не удалось прочитать текущую дату";
            if (dt.Length != 7)
            {
                OnSendMessage(errorMessage);
                return default(DateTime);
            }

            var hour = ConvertFromBcd(dt[1]);
            var min = ConvertFromBcd(dt[2]);
            var sec = ConvertFromBcd(dt[3]);
            var day = ConvertFromBcd(dt[4]);
            var mon = ConvertFromBcd(dt[5]);
            var year = ConvertFromBcd(dt[6]);

            try
            {
                var dateTime = new DateTime(year + 2000, mon, day, hour, min, sec);
                return dateTime;
            }
            catch
            {
                OnSendMessage(errorMessage);
                return default(DateTime);
            }
        }
        /// <summary>
        /// Прочитать архив за один час
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private IEnumerable<Data> ReadHalfHourBlock(Block block)
        {
            var result = new List<Data>();
            OnSendMessage("Читаем блок №{1} ({2}:00 - {3}:00) за {0}", block.Date.ToString("dd.MM.yyyy"), block.Number, block.Number * 4, (block.Number + 1) * 4);
            var answer = GetAnswer(0x37, new byte[] { block.Number, ConvertToBcd(block.Date.Day), ConvertToBcd(block.Date.Month), ConvertToBcd(block.Date.Year - 2000) });
            if(answer == null)
            {
                OnSendMessage("Блок №{0} не прочитан", block.Number);
            }
            OnSendMessage("Блок №{0} прочитан. Анализируем...", block.Number);

            double? previousValue = null;//для складывания получасовок в часы
            for (int i = 0; i < answer.Length; i = i + 3)
            {
                if (answer[i + 2] != 0)
                {
                    previousValue = null;
                    OnSendMessage("Запись за {0} {1}:{2} недействительна", block.Date.ToString("dd.MM.yyyy"), (block.Number * 4 + ((int)((i / 3) / 2))).ToString("00"), (((int)((i / 3) % 2))*30).ToString("00"));
                    continue;
                }
                double value = BitConverter.ToUInt16(answer, i) * 0.2;
                OnSendMessage("Запись за {0} {1}:{2} действительна", block.Date.ToString("dd.MM.yyyy"), (block.Number * 4 + ((int)((i / 3) / 2))).ToString("00"), (((int)((i / 3) % 2)) * 30).ToString("00"));
                if ((i % 2) == 0)
                {
                    previousValue = value;
                }
                else if (previousValue != null)//если данных за предыдущую получасовку нет - час склеить не получиться
                {
                    var data = new Data("A+", MeasuringUnitType.Wt, new DateTime(block.Date.Year, block.Date.Month, block.Date.Day, block.Number + i / 2, 0, 0), value + previousValue.Value);
                    result.Add(data);
                    OnSendMessage("Запись за {0} склеена из получасовок", data.Date.ToString("dd.MM.yyyy HH:mm"));
                    previousValue = null;
                }
            }
            return result;
        }
        private IEnumerable<Block> CreateBlocks(IEnumerable<DateTime> dates)
        {
            var result = new List<Block>();
            if (dates == null) return result;

            var datesList = dates.ToList();
            while (datesList.Any())
            {
                var date = datesList[0];

                //все получасовки сидят в группе из 8 получасовок. Надо определить к какой группе относиться 
                //запрашиваемый час

                byte halfHourGroup = (byte)(date.Hour / 4);
                result.Add(new Block { Date = date.Date, Number = halfHourGroup });
                datesList.RemoveAll(dt => dt.Date == date.Date && dt.Hour >= halfHourGroup * 4 && dt.Hour < (halfHourGroup + 1) * 4);
            }
            return result;
        }
    }
}
