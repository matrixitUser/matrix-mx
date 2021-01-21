using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private byte[] Send(byte[] data)
        {
            request(data);
            //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            byte[] buffer = new byte[] { };
            var timeout = 20000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }
            //log(string.Format("пришло {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            return buffer;
        }

    }

    enum Direction : byte
    {
        MasterToSlave = 0x02,
        SlaveToMaster = 0x06
    }
}
