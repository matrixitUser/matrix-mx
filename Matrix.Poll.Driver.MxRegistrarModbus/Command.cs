using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        public void SetNaCmd(string command)
        {
            if (command.StartsWith("setna="))
            {
                if (NetworkAddress.Count == 1 && NetworkAddress[0] == 0)
                {
                    log("Нельзя изменить сетевой адрес устройству с сетевый адресом 0 (широковещательный)", level: 1);
                    return;
                }
                var strValue = command.Substring(6);  //30, 500, 100, 200, 110
                byte val = 0;
                if (Byte.TryParse(strValue, out val) && (val > 0) && (val <= 250))
                {
                    var setna = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest(0x30002, 1, new List<byte>() { val })));
                    if (setna.success)
                    {
                        if (setna.Wrote > 0)
                        {
                            log(string.Format("Новый сетевой адрес успешно изменён: {0}", val), level: 1);
                        }
                        else
                        {
                            log("Не удалось установить сетевой адрес (нет доступа?)", level: 1);
                        }
                    }
                    else
                    {
                        log(string.Format("Не удалось установить сетевой адрес: {0}", setna.error), level: 1);
                    }
                }
                else
                {
                    log(string.Format("Новый сетевой адрес не распознан: должно быть число от 1 до 240, введено {0}", strValue), level: 1);
                }
            }
        }

        public void SetBkpCmd(string command, byte counters, ushort devid)
        {
            if (command.StartsWith("setbkp=")) //setbkp=30,50,100,200,110 => cnt=4: 30,50,100,200; cnt=8: 30,50,100,200,110,0,0,0
            {
                var newValues = new List<UInt32>();
                var newStringValues = command.Substring(7).Split(',');  //30, 500, 100, 200, 110

                //newValues = 30, 500, 100, 200, 110
                foreach (var strvalue in newStringValues)
                {
                    UInt32 val;
                    if (strvalue == "")
                    {
                        newValues.Add(0xFFFFFFFF);
                    }
                    else if (UInt32.TryParse(strvalue, out val))
                    {
                        newValues.Add(val);
                    }
                }

                //newValues = 30, 500, 100, 200, 110 OR newValues = 30, 500, 100, 200, 110, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF
                while (newValues.Count < counters)
                {
                    newValues.Add(0xFFFFFFFF);
                }

                //newData = 0,0,0,0x30, 0,0,0x1,0xf4, 0,0,0,0x64, 0,0,0,0xc8 OR 0,0,0,0x30, 0,0,0x1,0xf4, 0,0,0,0x64, 0,0,0,0xc8, 0,0,0,0x6e, 0,0,0,0 0,0,0,0, 0,0,0,0
                var newData = new List<byte>();
                for (var i = 0; i < counters; i++)
                {
                    newData.AddRange(BitConverter.GetBytes(newValues[i]).Reverse());
                }

                if (GetRegisterSet(devid).name == "new")
                {
                    var setbkp = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).Counter, (UInt16)(counters * 4), newData)));
                    if (setbkp.success)
                    {
                        if (setbkp.Wrote > 0)
                        {
                            log("Счётные регистры успешно установлены!", level: 1);
                        }
                        else
                        {
                            log("Не удалось установить счётные регистры (нет доступа?)", level: 1);
                        }
                    }
                    else
                    {
                        log(string.Format("Не удалось установить счётные регистры: {0}", setbkp.error), level: 1);
                    }
                }
                else
                {
                    var setbkp = Send(MakeWriteBkpRequest(DateTime.MinValue, devid, newValues.ToArray()));
                    if (setbkp.success)
                    {
                        log("Счётные регистры успешно установлены!", level: 1);
                    }
                    else
                    {
                        log(string.Format("Не удалось установить счётные регистры: {0}", setbkp.error), level: 1);
                    }

                }
            }

        }
    }
}
