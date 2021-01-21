using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.ModemPool
{
    class GsmModem
    {
        private SerialPort port;        

        private bool Check()
        {
            return port != null && port.IsOpen;
        }

        private bool Connect()
        {
            return true;
        }

        public void Call(string phone)
        {

        }

        public void Drop()
        {

        }
    }
}
