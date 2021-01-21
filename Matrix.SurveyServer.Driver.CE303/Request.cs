using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.CE303
{
    class Request
    {

        public List<byte> Data { get; private set; }
  
        public List<byte> R1 = new List<byte>(Encoding.Default.GetBytes("R1"));
        public List<byte> P1 = new List<byte>(Encoding.Default.GetBytes("P0"));
        public List<byte> address = null;

        public byte ComputeВCC(IList<byte> bytes, int begBCC)
        {
            byte bcc = 0;
            for (int i = begBCC; i < bytes.Count; i++)
            {
                bcc = (byte)(bcc + bytes[i]);
            }

            return (byte) (bcc);
        }

        public byte Calculate(byte[] buffer, int offset, int length)
        {
            byte CS = 0;
            for (int i = offset; i < (length + offset); i++) { CS += buffer[i]; }
            return (byte) (CS ^ 0xFF) ;
        }

        public Request(string identName)
        {
            Data = new List<byte>();
            address = new List<byte>(Encoding.Default.GetBytes(identName));
        }

        public virtual byte[] GetBytes()
        {

           return new byte[] { NAKL, VOPROS, 0x37, 0x32, 0x31, VOSKL,30,30,30,31, ETX, CR, LF };
        
        }
    }
}
