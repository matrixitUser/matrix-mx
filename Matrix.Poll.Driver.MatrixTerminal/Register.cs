using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.MatrixTerminal
{
    public partial class Driver
    {
        byte[] MakeRegisterRequest(UInt32 register, UInt16 registerCount)
        {
            var Data = new List<byte>();

            var function = (byte)(register >> 16);
            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            return MakeBaseRequest(function, Data);
        }

        byte[] MakeWriteHoldingRegisterRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(16, Data);
        }
        byte[] MakeLightRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(73, Data);
        }

        byte[] MakeValveControlRequest(UInt32 register, UInt16 registerCount, List<byte> wdata) //пока не используется 05.08.2019
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(73, Data); //надо поменять номер функции
        }

        byte[] MakeAbnormalRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(75, Data); //4b
        }
        byte[] MakeEventsRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(76, Data); //4c
        }
        byte[] MakeAstronRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));

            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));

            Data.AddRange(wdata);

            return MakeBaseRequest(74, Data);
        }
       
        byte[] MakeDHTRequest(UInt32 register, UInt16 registerCount, List<byte> wdata)
        {
            var Data = new List<byte>();

            UInt16 startRegister = (UInt16)register;
            Data.Add(Helper.GetHighByte(startRegister));
            Data.Add(Helper.GetLowByte(startRegister));
            /*
            Data.Add(Helper.GetHighByte(registerCount));
            Data.Add(Helper.GetLowByte(registerCount));
            
            Data.AddRange(wdata);
            */
            return MakeBaseRequest(75, null);
        }
        dynamic ParseRegisterResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            answer.Length = answer.Body[0];
            if (answer.Body.Length < (answer.Length + 1))
            {
                answer.success = false;
                answer.error = "пакет короток";
                answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
            }
            else
            {
                byte[] body = answer.Body;
                answer.Register = body.ToList().Skip(1).ToArray();
            }

            return answer;
        }

        dynamic ParseWriteHoldingRegisterResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            answer.Wrote = Helper.ToUInt16(answer.Body, 2);
            return answer;
        }
    }
}
