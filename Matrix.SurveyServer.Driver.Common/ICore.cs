using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
	public delegate void ResponseHandler(byte[] data);
	public interface ICore : IDisposable
	{
		void SendData(byte[] data, byte port);
		void SendCommand(byte[] data, byte commandNumber);
		event ResponseHandler DataReceived;
		bool IsConnected { get; }
		event ResponseHandler CommandReceived;
		event Action<int> Ping;
		event Action<int> TrafficAdded;
	}
}
