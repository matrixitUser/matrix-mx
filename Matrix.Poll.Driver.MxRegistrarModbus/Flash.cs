using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
//using System.Threading;
using System.IO;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        public enum ModbusFunction
        {
            MODBUS_USER_FUNCTION_BOOTLOADER_BKPSIZE = 0x66,//102
            MODBUS_USER_FUNCTION_BOOTLOADER_BKPREAD = 0x67,
            MODBUS_USER_FUNCTION_BOOTLOADER_BKPERASE = 0x68,
            MODBUS_USER_FUNCTION_BOOTLOADER_BKPWRITE = 0x69,

            MODBUS_USER_FUNCTION_BOOTLOADER_PSIZE = 0x6A,
            MODBUS_USER_FUNCTION_BOOTLOADER_PREAD = 0x6B,
            MODBUS_USER_FUNCTION_BOOTLOADER_PERASE = 0x6C,
            MODBUS_USER_FUNCTION_BOOTLOADER_PWRITE = 0x6D,
            MODBUS_USER_FUNCTION_BOOTLOADER_PSTART = 0x6E//110
        }

        public enum FlashWriteOption
        {
            None,
            StartOnly,
            EraseOnly,
            WriteOnly,
            BkpErase,
            BkpRead,
            BkpWrite
        }

        bool processStarted = false;
        byte[] fileBytes;
        List<byte> received = new List<byte>();
        FlashWriteOption woption;

        #region Request&Parse
        /*----------------------------------------Request----------------------------------------*/
        byte[] MakePassRequest()
        {
            var Data = (new byte[] { 0x04, 0x00, 0x00, 0x18 }).ToList(); ;

            var function = (byte)0x10;
            string password = "X7]JSAm5pxKoiLMzMOr0C$5K";
            Data.AddRange(ASCIIEncoding.ASCII.GetBytes(password));

            return MakeBaseRequest(function, Data);
        }
        dynamic ParsePassResponse(dynamic answer)
        {
            return answer;
        }

        byte[] MakeBootloaderRequest()
        {
            var Data = (new byte[] { 0x3F, 0xFC, 0x00, 0x04 }).ToList();

            var function = (byte)0x10;
            Data.AddRange(BitConverter.GetBytes(0xAAAA5555).Reverse());

            return MakeBaseRequest(function, Data);
        }
        dynamic ParseBootloaderResponse(dynamic answer)
        {
            return answer;
        }

        byte[] MakeBootloaderPsizeRequest()
        {
            var Data = new List<byte>();

            var function = (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PSIZE;
            //Data.Add(0x00);

            return MakeBaseRequest(function, Data);
        }
        dynamic ParseBootloaderPsizeResponse(dynamic answer)
        {
            if (answer.Body.Length == 4)
            {
                if (answer.Function == (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PSIZE)
                {
                    //TODO разобраться с reverse()
                    List<byte> tmp = new List<byte>(answer.Body);
                    log(string.Format("{0}", string.Join(",", tmp.Select(b => b.ToString("X2")))), level: 1);
                    tmp.Reverse();
                    log(string.Format("{0}", string.Join(",", tmp.Select(b => b.ToString("X2")))), level: 1);
                    UInt32 size = BitConverter.ToUInt32(tmp.ToArray(), 0);
                    log(string.Format("максимальный размер программы = {0}", size), 1);

                    if (size == 0)
                    {
                        log("не удалось получить максимальный размер программы", 1);
                        processStarted = false;
                        answer.success = false;
                        return answer;
                    }
                }
            }
            else
            {
                answer.success = false;
            }
            return answer;
        }

        byte[] MakeBootloaderBkpEraseRequest()
        {
            var Data = new List<byte>();

            var function = (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PERASE;
            Data.AddRange(BitConverter.GetBytes((UInt32)0x00000000).Reverse());
            Data.AddRange(BitConverter.GetBytes((UInt32)fileBytes.Length).Reverse());

            return MakeBaseRequest(function, Data);
        }
        dynamic ParseBkpEraseResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            if (answer.Body.Length != 8)
            {
                answer.success = false;
                answer.error = "не удачная очистка";
                answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
            }
            else
            {
                List<byte> tmp = new List<byte>(answer.Body);
                tmp.Reverse();
                UInt32 receivedErase = BitConverter.ToUInt32(tmp.ToArray(), 0);//Skip(4).Take(4).
                log(string.Format("очищено {0} байт", receivedErase), 1);
            }

            return answer;
        }

        byte[] MakeBootloaderPwriteRequest(UInt32 addr, UInt16 step, int fileBytesOffset)
        {
            var Data = new List<byte>();

            var function = (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PWRITE;

            Data.AddRange(BitConverter.GetBytes(addr).Reverse());
            Data.AddRange(BitConverter.GetBytes(step).Reverse());
            Data.AddRange(fileBytes.Skip(fileBytesOffset).Take(step));

            return MakeBaseRequest(function, Data);
        }
        dynamic ParseBootloaderPwriteResponse(dynamic answer, UInt32 addr, UInt16 step, int fileBytesOffset)
        {
            if (answer.Body.Length == 6)
            {
                if (answer.Function == (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PWRITE)
                {
                    List<byte> tmp1 = new List<byte>(answer.Body);
                    List<byte> tmp2 = new List<byte>(answer.Body);
                    tmp1.Reverse();
                    tmp2.Reverse();
                    UInt32 receivedAddr = BitConverter.ToUInt32(tmp1.ToArray(), 0);
                    UInt16 receivedStep = BitConverter.ToUInt16(tmp2.ToArray(), 0);
                    if ((receivedAddr == addr) && (receivedStep == step))
                    {
                        int recorded = fileBytesOffset + step - 1;
                        log(string.Format($"[{(double)recorded / fileBytes.Length * 100.0:000.0}%] записаны байты {fileBytesOffset}-{recorded} из {fileBytes.Length}"), 1);
                        answer.success = true;
                        //attempts = 2;
                        //continue;
                    }
                }
            }
            else
            {
                answer.success = false;
            }
            return answer;
        }

        byte[] MakeBootloaderPstartRequest()
        {
            var Data = new List<byte>();

            var function = (byte)ModbusFunction.MODBUS_USER_FUNCTION_BOOTLOADER_PSTART;

            UInt32 sizeProgram = (UInt32)fileBytes.Length;
            var crc = Crc.Calc(fileBytes, new Crc16Modbus());
            log(string.Format("коммит размер={0} (0x{0:X8}) crc16 прошивки=0x{1:X2}{2:X2}", sizeProgram, crc.CrcData[1], crc.CrcData[0]), 1);

            Data.AddRange(BitConverter.GetBytes(sizeProgram).Reverse());
            Data.Add(crc.CrcData[1]);
            Data.Add(crc.CrcData[0]);

            return MakeBaseRequest(function, Data);
        }
        dynamic ParseBootloaderPstartResponse(dynamic answer)
        {
            List<byte> tmp = new List<byte>(answer.Body);
            log(string.Format("numReg from MCU: {0}", string.Join(",", tmp.Select(b => b.ToString("X2")))), level: 1);

            UInt32 sizeProgram = (UInt32)fileBytes.Length;
            byte upper1 = 0x00;
            byte lower1 = 0x00;
            byte upper2 = (byte)(sizeProgram >> 8);
            byte lower2 = (byte)(sizeProgram & 0xFF);
            List<byte> tmp2 = new List<byte>(new byte[] { upper1, lower1, upper2, lower2 });
            log(string.Format("numBytes from fileBytes: {0}", string.Join(",", tmp2.Select(b => b.ToString("X2")))), level: 1);
            byte[] tmp3 = tmp.ToArray();
            byte[] tmp4 = tmp2.ToArray();

            if (tmp3.SequenceEqual(tmp4))
            {
                log("Запись прошивки прошла успешно", 1);
            }
            else
            {
                log("Запись прошивки не удалась", 1);
            }

            return answer;
        }
        /*----------------------------------------Request----------------------------------------*/
        #endregion

        #region startFlash
        public void StartFlash(string base64String, string outputFilename, string inputBkpFilename, string outputBkpFilename, FlashWriteOption option = FlashWriteOption.None) //string inputFilename
        {
            if (!processStarted)
            {
                fileBytes = Convert.FromBase64String(base64String);
                processStarted = true;
                this.woption = option;
                try
                {
                    Process();//Добавлено
                }
                catch (Exception ex)
                {
                    log((string.Format("[{0}] процесс обновления прошивки НЕ запущен: {1}", this, ex.Message)), 1);
                }
            }
            else
            {
                processStarted = false;
                log(string.Format("[{0}] процесс обновления прошивки остановлен", this), 1);
            }
        }
        #endregion

        #region Process
        public void Process()
        {
            if(NetworkAddress.Count != 12)
            {
                log("Введите CID вместо сетевого адреса", 1);
                return;
            }
            #region BootloaderModeChecking
            dynamic flashVer = GetFlashVer();
            
            if (!flashVer.success)
            {
                log($"flashVer.success={flashVer.success}", 1);
                log("Возможно контроллер находится в режиме Bootloader", 1);
            }
            else
            {
                var ver = (int)flashVer.ver;
                if (ver != 0)
                {
                    log("Контроллер находится в режиме прошивки", 1);
                    log(string.Format("Статус получения версии оборудования - {0}, версия оборудования - {1}", flashVer.success, flashVer.ver), 1);
                    //Отправка pass2
                    dynamic resultPass;
                    {
                        resultPass = ParsePassResponse(Send(MakePassRequest()));
                    }

                    //Переход в bootloader
                    dynamic resultBoot;
                    {
                        resultBoot = ParseBootloaderResponse(Send(MakeBootloaderRequest()));
                    }
                }
                else
                {
                    log("Контроллер находится в режиме Bootloader", 1);
                    log(string.Format("Статус получения версии оборудования - {0}, версия оборудования - {1}", flashVer.success, flashVer.ver), 1);
                }
            }
           
            #endregion

            log("начало процесса", 1);

            #region firmawareChecking
            // Проверка на максимальный размер файла прошивки для записи во флеш.
            dynamic resultCheck;

            {
                resultCheck = ParseBootloaderPsizeResponse(Send(MakeBootloaderPsizeRequest()));
            }
            #endregion

            /////
            // кусок файла для считывания и записи в МК (количество байт).
            UInt16 fileBytesStep = 128;

            // fileBytes - файл прошивки МК в виде байтов.
            if ((fileBytes != null) && (fileBytes.Length > 0))
            {
                dynamic resultErase;

                // Сначала очистка памяти МК.
                if (woption == FlashWriteOption.None)
                {
                    #region flashErase
                    log("очистка памяти", 1);
                    resultErase = ParseBkpEraseResponse(Send(MakeBootloaderBkpEraseRequest()));
                    if (resultErase.success)
                    {
                        List<byte> tmp = new List<byte>(resultErase.Body);
                        tmp.Reverse();
                        UInt32 receivedErase = BitConverter.ToUInt32(tmp.ToArray(), 0);
                        log(string.Format("очищено {0} байт", receivedErase), 1);
                        //Thread.Sleep(500); TODO доделать
                    }
                    else
                    {
                        return;
                    }
                }
                #endregion

                dynamic resultWrite;

                if (woption == FlashWriteOption.None)
                {
                    int fileBytesOffset;
                    int attempts = 2;

                    #region flashWrite
                    // Запись в МК.
                    log((string.Format("[{0}] процесс обновления прошивки запущен", this)), level: 1);
                    for (fileBytesOffset = 0; (fileBytesOffset != fileBytes.Length) && (attempts > 0);)
                    {
                        UInt32 addr = (UInt32)fileBytesOffset;
                        UInt16 step = ((fileBytes.Length - fileBytesOffset) > fileBytesStep) ? fileBytesStep : (UInt16)(fileBytes.Length - fileBytesOffset);

                        resultWrite = ParseBootloaderPwriteResponse(Send(MakeBootloaderPwriteRequest(addr, step, fileBytesOffset)), addr, step, fileBytesOffset);
                        if (resultWrite.success)
                        {
                            log(string.Format("Записаны байты {0}-{1}", fileBytesOffset, fileBytesOffset + step - 1), 1);
                            fileBytesOffset += step;
                            attempts = 4;
                        }
                        else
                        {
                            log(string.Format("неудачная попытка записи {0}-{1}", fileBytesOffset, fileBytesOffset + step - 1), 1);
                            attempts--;
                        }
                    }
                    #endregion

                    #region pStart
                    // program start
                    dynamic resultStart;
                    if (attempts > 0)
                    {
                        resultStart = ParseBootloaderPstartResponse(Send(MakeBootloaderPstartRequest()));
                    }
                    else
                    {
                        log("отмена процесса", 1);
                    }
                    #endregion
                }
            }
            processStarted = false;

        }
        #endregion
    }
}
