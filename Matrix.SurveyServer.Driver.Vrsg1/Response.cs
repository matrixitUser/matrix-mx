//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Vrsg1
//{
//    public class Response
//    {
//        public Response(byte[] data)
//        {
//            if (!Crc.Check(data, new Crc16Modbus())) throw new Exception("не сошлась контрольная сумма");
//            NetworkAddress = data[0];
//            Function = data[1];

//            //modbus error
//            if (Function > 0x80)
//            {
//                var exceptionCode = (ModbusExceptionCode)data[2];
//                throw new Exception(string.Format("устройство вернуло ошибку: {0}", exceptionCode));
//            }
//        }

//        public byte NetworkAddress { get; private set; }
//        public byte Function { get; private set; }
//    }


//    enum ModbusExceptionCode : byte
//    {
//        ILLEGAL_FUNCTION = 0x01,
//        ILLEGAL_DATA_ADDRESS = 0x02,
//        ILLEGAL_DATA_VALUE = 0x03,
//        FAILURE_IN_ASSOCIATED_DEVICEE = 0x04,
//        ACKNOWLEDGE = 0x05,
//        SLAVE_DEVICE_BUSY = 0x06,
//        MEMORY_PARITY_ERROR = 0x07,
//        GATEWAY_PATH_UNAVAILABLE = 0x0a,
//        GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
//    }
//}
