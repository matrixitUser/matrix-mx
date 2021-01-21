using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
        byte[] MakeBaseRequest(byte function, List<byte> data = null)
        {
            if (data == null)
            {
                data = new List<byte>();
            }

            List<byte> bytes = new List<byte>();
            bytes.Add(NetworkAddress);
            bytes.Add(function);

            bytes.AddRange(data);

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.AddRange(crc.CrcData);

            bytes.Insert(0, 0x0ff);
            bytes.Insert(0, 0x0ff);

            return bytes.ToArray();
        }


        // READ

        byte[] MakeReadRequest(short startRegister, short registerCount)
        {
            var Frame = new List<byte>();

            Frame.Add(Helper.GetHighByte(startRegister));	//начальный регистр
            Frame.Add(Helper.GetLowByte(startRegister));

            Frame.Add(Helper.GetHighByte(registerCount));	//количество регистров
            Frame.Add(Helper.GetLowByte(registerCount));

            return MakeBaseRequest(0x03, Frame);
        }

        dynamic ParseReadResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            var body = (byte[])answer.Body;

            if (body.Length <= 1)
            {
                answer.success = false;
                answer.error = "пакет короток";
                answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
            }

            if (answer.Function >= 0x80)
            {
                answer.code = body[0];
                answer.success = false;
                answer.error = "при чтении возникло исключение - ";
                answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                switch ((byte)answer.code)
                {
                    case 3:
                        answer.error += "архив в приборе отсутствует";
                        break;
                    case 5:
                        answer.error += "зафиксировано изменение схемы измерения";
                        break;
                    case 7:
                        answer.error += "дискретные выходы не являются управляемыми дистанционно";
                        break;
                    default:
                        answer.error += "код:" + answer.code;
                        break;
                }
                return answer;
            }

            answer.Length = body[0];
            body = body.ToList().Skip(1).ToArray();

            if (body.Length != answer.Length)
            {
                answer.success = false;
                answer.error = "полученное число байт не соответствует заявленной в байте КБ";
                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
            }

            answer.Body = body;

            return answer;
        }


        // WRITE

        byte[] MakeWriteRequest(short startRegister, short registerCount, byte[] data)
        {
            if (data.Length > byte.MaxValue) throw new Exception("количество байт данных превышает максимально возможное");

            var Frame = new List<byte>();

            Frame.Add(Helper.GetHighByte(startRegister));
            Frame.Add(Helper.GetLowByte(startRegister));

            Frame.Add(Helper.GetHighByte(registerCount));
            Frame.Add(Helper.GetLowByte(registerCount));

            Frame.Add((byte)data.Length);
            Frame.AddRange(data);

            return MakeBaseRequest(16, Frame);
        }

        dynamic ParseWriteResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            var body = (byte[])answer.Body;

            if (body.Length <= 1)
            {
                answer.success = false;
                answer.error = "пакет короток";
                answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
            }

            //if (answer.NetworkAddress == 0x83)
            //{
            //    answer.success = false;
            //    answer.error = string.Format("вычислитель вернул ошибку; код={0}", body[0]);
            //}

            if (answer.Function >= 0x80)
            {
                answer.code = body[0];
                answer.success = false;
                answer.error = "при записи возникло исключение - ";
                answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                switch ((byte)answer.code)
                {
                    case 2:
                        answer.error += "задан несуществующий элемент/тип значений";
                        break;
                    case 3:
                        answer.error += "в архиве отсутствуют данные за эту дату";
                        break;
                    case 5:
                        answer.error += "размер массива больше максимально возможного";
                        break;
                    //case 7:
                    //    answer.error += "дискретные выходы не являются управляемыми дистанционно";
                    //    break;
                    case 7:
                        answer.error += "дискретные выходы не являются управляемыми дистанционно";
                        break;
                    default:
                        answer.error += "код:" + answer.code;
                        break;
                }
            }

            return answer;
        }









        /// <summary>
        /// запрос на чтение перечня активных элементов данных
        /// см. док. п. 4.1 стр. 13
        /// </summary>
        byte[] MakeReadActiveElementsRequest()
        {
            return MakeReadRequest(0x3ffc, 0x0000);
        }

        dynamic ParseReadActiveElementsResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;

            ///0 na
            ///1 fn
            ///2 len
            ///3 data

            read.ActiveElements = new List<dynamic>();
            for (int offset = 0; offset < read.Length; offset += 6)
            {
                dynamic activeElement = new ExpandoObject();
                activeElement.Address = BitConverter.ToInt16(read.Body, offset + 0);
                activeElement.Length = BitConverter.ToInt16(read.Body, offset + 4);
                read.ActiveElements.Add(activeElement);
            }

            return read;
        }


        /// <summary>
        /// запрос на запись перечня элементов для чтения
        /// см. док. п. 4.2 стр. 13
        /// </summary>
        byte[] MakeWriteElementsRequest(IEnumerable<dynamic> elements)
        {
            List<byte> bytes = new List<byte>();
            foreach (var element in elements)
            {
                var addr = element.Address;
                var len = element.Length;

                var Data = new List<byte>();

                Data.Add(Helper.GetLowByte(addr));
                Data.Add(Helper.GetHighByte(addr));

                Data.Add(Helper.GetLowByte(0x4000));
                Data.Add(Helper.GetHighByte(0x4000));

                Data.Add(Helper.GetLowByte(len));
                Data.Add(Helper.GetHighByte(len));

                bytes.AddRange(Data);
            }

            return MakeWriteRequest(0x3fff, 0x0000, bytes.ToArray());
        }














        /// <summary>
        /// запрос на чтение служебной информации
        /// см. док. п. 4.6 стр. 17
        /// </summary>
        byte[] MakeReadInfoRequest()
        {
            return MakeReadRequest(0x3ff9, 0x0000);
        }

        /// <summary>
        /// запрос на чтение данных 
        /// см. док. п. 4.5 стр. 16
        /// </summary>
        byte[] MakeReadDataRequest()
        {
            return MakeReadRequest(0x3ffe, 0x0000);
        }

        /// <summary>
        /// запрос на запись типа значений
        /// см. док. п. 4.3 стр. 14
        /// </summary>
        /// 
        byte[] MakeWriteValueTypeRequest(ValueType archiveType)
        {
            return MakeWriteRequest(0x3ffd, 0x0000, new byte[] 
			{ 
				Helper.GetLowByte((short)archiveType),
				Helper.GetHighByte((short)archiveType)
			});
        }

        /// <summary>
        /// запрос на чтение текущей даты и времени
        /// см. док. п. 4.14 стр. 22
        /// </summary>
        byte[] MakeReadCurrentDateRequest()
        {
            return MakeReadRequest(0x3ffb, 0x0000);
        }

        dynamic ParseReadCurrentDateResponse(dynamic answer, byte version)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;

            var body = (byte[])read.Body;

            int day = body[0];
            int month = body[1];
            int year = body[2] + 2000;
            int hour = body[3];
            int minute = version >= 0x27 ? body[4] : 0;
            int second = version >= 0x27 ? body[5] : 0;

            read.Date = new DateTime(year, month, day, hour, minute, second);
            return read;
        }

        /// <summary>
        /// запрос на запись даты
        /// см. док. п. 4.4 стр. 15
        /// </summary>
        byte[] MakeWriteDateRequest(DateTime date)
        {
            return MakeWriteRequest(0x3ffb, 0x0000, new byte[]
			{
				(byte)date.Day,
				(byte)date.Month,
				(byte)(date.Year-2000),
				(byte)date.Hour,
			});
        }

        //

        byte[] MakeReadPropertiesUnitsRequest()
        {
            return MakeReadDataRequest();
        }

        //

        byte[] MakeReadPropertiesFracsRequest()
        {
            return MakeReadDataRequest();
        }

        //

        byte[] MakeReadArchiveRequest()
        {
            return MakeReadDataRequest();
        }

        //// PARSE

        dynamic ParseReadInfoResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;
            var body = (byte[])read.Body;

            read.Constants = new List<dynamic>();

            if (read.Length > 3)
            {
                read.Version = body[0];
                read.measSch1 = BitConverter.ToUInt16(body, 1);
                read.measSch2 = BitConverter.ToUInt16(body, 3);
                read.FactoryNumber = Encoding.ASCII.GetString(body, 5, 8);
                read.TotalDay = body[14];
                read.NA = body[13];
                read.MI = body[15];

                read.connSch1 = (read.measSch1 & 0x1e00) >> 9;
                read.tr3use1 = (read.measSch1 & 0x0180) >> 7;
                read.t5use1 = (read.measSch1 & 0x0060) >> 5;

                read.connSch2 = (read.measSch2 & 0x1e00) >> 9;
                read.tr3use2 = (read.measSch2 & 0x0180) >> 7;
                read.t5use2 = (read.measSch2 & 0x0060) >> 5;
            }
            else
            {
                read.Version = 0x10;
                read.TotalDay = body[0];
                read.FactoryNumber = "";
            }

            return read;
        }


        dynamic ParseReadServerVersionResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;
            //var body = ((List<byte>)read.Body).ToArray();

            read.success = false;

            if (read.data.Length >= 65)
            {
                read.version = read.data[65];
            //    log("Версия сервера = " + read.Body[65].ToString());
                read.success = true;
            }

            //if (read.Length >= 65)
            //{
            //    log("Либо же " + read.Body[65].ToString());
            //    read.success = true;
            //}

            //if(read.Length >= 65)
            //{
            //    body[65];
            //}

            return read;
        }

        dynamic ParseReadPropertiesUnitsResponse(dynamic answer, IEnumerable<dynamic> elements, int serverVersion)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;
            var body = (byte[])read.Body;

            var elementIndex = 0;
            var offset = 0;

            read.Units = new List<dynamic>();

            while (offset < read.Length || elementIndex < elements.Count())
            {
                var element = elements.ElementAt(elementIndex);

                ushort len;
                ushort aoff;

                if (serverVersion == 0)
                {
                    len = 7;
                    aoff = 0;
                }
                else
                {
                    len = body[offset];
                    aoff = 2;
                }
                var unit = Encoding.GetEncoding(866).GetString(body, offset + aoff, len);

                dynamic u = new ExpandoObject();
                u.Unit = unit;
                u.Address = element.Address;

                read.Units.Add(u);

                //log(string.Format("Адрес={0} Ед.измер={1}", u.Address, u.Unit));

                elementIndex++;
                offset += 2 + len + 2;
            }

            return read;
        }

        dynamic ParseReadPropertiesMultiplierResponse(dynamic answer, IEnumerable<dynamic> elements)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;
            var body = (byte[])read.Body;

            var elementIndex = 0;
            var offset = 0;

            read.Fracs = new List<dynamic>();

            while (offset < read.Length && elementIndex < elements.Count())
            {
                var element = elements.ElementAt(elementIndex);
                var frac = body[offset];

                dynamic fracElement = new ExpandoObject();

                fracElement.Frac = frac;
                fracElement.Address = element.Address;

                switch ((short)fracElement.Address)
                {
                    case 1:
                        fracElement.AddressReference = 1;
                        break;
                }

                read.Fracs.Add(fracElement);

                //log(string.Format("Адрес={0} Степень={1}", fracElement.Address, fracElement.Frac));

                elementIndex++;
                offset += 1 + 2;
            }

            return read;
        }

        /// <summary>
        /// тип значений
        /// см. док. п. 4.3 стр. 14
        /// </summary>
        public enum ValueType : short
        {
            /// <summary>
            /// часовой архив
            /// </summary>
            Hour = 0x0000,
            /// <summary>
            /// суточный архив
            /// </summary>
            Day = 0x0001,
            /// <summary>
            /// месячный архив
            /// </summary>
            Month = 0x0002,
            /// <summary>
            /// итоговый архив
            /// </summary>
            Total = 0x0003,
            /// <summary>
            /// текущие значения
            /// </summary>
            Current = 0x0004,
            /// <summary>
            /// итоговые текущие
            /// </summary>
            TotalCurrent = 0x0005,
            /// <summary>
            /// свойства
            /// </summary>
            Properties = 0x0006
        }
    }

    //public class FracElement
    //{
    //    public byte Frac { get; private set; }
    //    public short Address { get; private set; }

    //    public short AddressReference { get; private set; }

    //    public FracElement(byte frac, short address)
    //    {
    //        Frac = frac;
    //        Address = address;

    //        switch (Address)
    //        {
    //            case 1:
    //                AddressReference = 1;
    //                break;
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return string.Format("addr={0}({1}) frac={2}", Address, AddressReference, Frac);
    //    }
    //}

    //public class UnitElement
    //{
    //    public string Unit { get; private set; }
    //    public short Address { get; private set; }

    //    public UnitElement(string unit, short address)
    //    {
    //        Unit = unit;
    //        Address = address;
    //    }

    //    public override string ToString()
    //    {
    //        return string.Format("addr={0} unit={1}", Address, Unit);
    //    }
    //}
}
