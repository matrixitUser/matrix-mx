using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    static class Helper
    {
        public static dynamic BuildMessage(string what)
        {
            dynamic message = new ExpandoObject();
            message.head = new ExpandoObject();
            message.head.what = what;
            message.body = new ExpandoObject();
            return message;
        }
        public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
        {
            return source.Skip(start).Take(count).Reverse().ToArray();
        }
        public static UInt16 ToUInt16(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToUInt16(Reverse(data, startIndex, 2), 0);
        }
        public static UInt32 ToUInt32(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
        }
    }
}
