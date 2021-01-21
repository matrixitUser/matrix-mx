using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public enum DeviceError
    {
        NO_ERROR = 0,
        NO_ANSWER,
        TOO_SHORT_ANSWER,
        ANSWER_LENGTH_ERROR,
        ADDRESS_ERROR,
        CRC_ERROR,
        DEVICE_EXCEPTION
    };

    public class DriverException : ApplicationException
    {
        private string message;
        public DeviceError DeviceError { get; private set; }
        public DriverException(string message, DeviceError deviceError)
        {
            this.DeviceError = deviceError;
            this.message = message;
        }
        public override string Message
        {
            get
            {
                return message;
            }
        }
    }
}
