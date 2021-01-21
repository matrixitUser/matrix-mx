//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    class Request3 : Request
//    {
//        public short Register { get; private set; }
//        public short RegisterCount { get; private set; }

//        public Request3(byte networkAddress, short register, short registerCount)
//            : base(networkAddress, 3)
//        {
//            Register = register;
//            RegisterCount = registerCount;
//            Data.Add(Helper.GetHighByte(Register));
//            Data.Add(Helper.GetLowByte(Register));
//            Data.Add(Helper.GetHighByte(RegisterCount));
//            Data.Add(Helper.GetLowByte(RegisterCount));
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос modbus регистров c {0} в количестве {1}", Register, RegisterCount);
//        }
//    }
//}
