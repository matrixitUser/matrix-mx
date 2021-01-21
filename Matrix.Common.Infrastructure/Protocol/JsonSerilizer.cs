using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Matrix.Common.Infrastructure.Protocol.Messages;
using Ionic.Zip;
using System.IO;

namespace Matrix.Common.Infrastructure.Protocol
{
    public class JsonSerilizer : ISerializer
    {
       

        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, };
        public byte[] SerializeMessage(DoMessage message)
        {
            if (message == null)
            {
                return new byte[0];
            }

            IEnumerable<byte> binary = null;            

            var jm = new JsonMessage { Message = message };
            string json = JsonConvert.SerializeObject(jm, serializerSettings);

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            if (binary != null)
            {
                List<byte> b = bytes.ToList();
                b.Add(0x03);
                b.AddRange(binary);
                bytes = b.ToArray();
            }            

            return bytes;
        }

        public DoMessage DeserializeMessage(byte[] data)
        {
            if (data == null) return null;           

            byte[] binary = null;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x03)
                {
                    binary = data.Skip(i + 1).ToArray();
                    data = data.Take(i).ToArray();
                }
            }

            string json = Encoding.UTF8.GetString(data);
            var jm = JsonConvert.DeserializeObject<JsonMessage>(json, serializerSettings);
            if (jm == null) return null;
            
            return jm.Message;
        }

        private class JsonMessage
        {
            public DoMessage Message { get; set; }
        }
    }
}
