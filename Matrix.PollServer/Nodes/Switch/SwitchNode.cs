//using log4net;
//using System;
//using System.Collections.Generic;
//using System.Dynamic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Matrix.PollServer.Nodes.Switch
//{
//    partial class SwitchNode : BaseNode
//    {
//        private readonly static ILog log = LogManager.GetLogger(typeof(SwitchNode));

//        public SwitchNode(ExpandoObject content)
//            : base(content)
//        {

//        }

//        public byte NetworkAddress
//        {
//            get
//            {
//                // return (byte)(body.networkAddress ?? 1);
//                return 1;
//            }
//        }

//        private bool IsPortUsed(byte port)
//        {
//            //int count = routes.Values.Select(v => v.Item1).Where(r => (byte)r.Arg["port"] == port).Count();
//            //return count > 0;
//            return false;
//        }

//        private bool isOpen = false;

//        public override PrepareResult Prepare(Relation input, Guid route, Dictionary<string, object> args)
//        {
//            log.Info("Подготовка switch");
//            var port = GetInputPort(input.Start);
//            if (!port.success)
//            {
//                log.Error(string.Format("Подготовка Switch'а потерпела крах, {0}", port.error));
//                return new PrepareResult(PrepareState.TryLater, "");
//            }

//            if (IsPortUsed((byte)port.body))
//            {
//                log.Debug(string.Format("Узел {0} занят", this.Id));
//                return new PrepareResult(PrepareState.TryLater, "");
//            }

//            var isReady = base.Prepare(input, route, args);

//            if (isReady.PrepareState == PrepareState.Success)
//            {
//                byte[] bytes = MakeOpenPortRequest(NetworkAddress, (byte)port.body);

//                log.Info(string.Format("switch: отправлено {0}", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//                base.Accept(null, route, bytes, true);

//                var timeout = 3000;
//                isOpen = false;
//                var waitPeriod = 100;
//                while (!isOpen && timeout > 0)
//                {
//                    Thread.Sleep(waitPeriod);
//                    timeout -= waitPeriod;
//                }
//                log.Info(string.Format("switch: готовность {0}", isOpen));
//                if (isOpen)
//                    return new PrepareResult(PrepareState.Success, "");
//                else
//                    return new PrepareResult(PrepareState.TryLater, "");
//            }
//            LeaveRoute(route);
//            //RemoveRoute(route);
//            return new PrepareResult(PrepareState.TryLater, "");
//        }

//        public override bool Accept(BaseNode sender, Guid routeId, byte[] bytes, bool toConnection)
//        {
//            log.Info(string.Format("получено от {1}: {0}", string.Join(",", bytes.Select(b => b.ToString("X2"))), toConnection ? "тюба" : "конекшена"));
//            byte[] data = null;
//            if (toConnection)
//            {
//                var port = GetInputPort(sender);
//                if (!port.success)
//                {
//                    log.Error(string.Format("передача данных потерпела крах, {0}", port.error));
//                    return false;
//                }
//                data = MakeSwitchRequest(NetworkAddress, (byte)port.body, 0x02, bytes);
//            }
//            else
//            {
//                var answer = ParseSwitchResponse(bytes);
//                if (!answer.success)
//                {
//                    log.Error(string.Format("передача данных потерпела крах, {0}", answer.error));
//                    return false;
//                }
//                if (answer.body.command == 0x01)
//                {
//                    if (answer.body.data.Length > 0 &&
//                        (answer.body.data[0] == 0x00 || answer.body.data[0] == 0x83))
//                        isOpen = true;
//                    else
//                        isOpen = false;

//                    return true;
//                }
//                data = answer.body.data;

//            }
//            return base.Accept(sender, routeId, data, toConnection);
//        }

//        //private List<byte> packageBuffer = new List<byte>();
//        //private IEnumerable<byte> GetPackage(IEnumerable<byte> buffer)
//        //{
//        //    if (buffer == null) return new byte[] { };
//        //    var len = BitConverter.ToInt16(buffer.ToArray(), 1);

//        //    buffer = buffer.SkipWhile(b => b != NetworkAddress);

//        //    if (buffer.Count() < len) return buffer;

//        //    var frame = buffer.Take(len);

//        //   // var response = new Response(frame.ToArray());
//        //    var response = ParseSwitchResponse(buffer.ToArray());
//        //    if (!response.success)
//        //    {
//        //        log.Error(string.Format("передача данных потерпела крах, {0}", response.error));
//        //        return response;
//        //    }

//        //    switch ((byte)response.body.channel)
//        //    {
//        //        case 0x01:
//        //        case 0x02:
//        //            switch ((byte)response.body.command)
//        //            {
//        //                case 0x01:
//        //                    isOpen = true;
//        //                    break;
//        //                case 0x03:
//        //                    {
//        //                        DoForInputsByPort(response.Channel, "out-bytes", new Dictionary<string, object> { { "bytes", response.Body } });
//        //                        break;
//        //                    }
//        //            }
//        //            break;

//        //        case 0x7f:
//        //            {
//        //                switch (response.Command)
//        //                {
//        //                    case 0x05:
//        //                        var speed = BitConverter.ToInt32(response.Body, 9);

//        //                        RaiseDataRecordsAppeared(MakeLog(string.Format("скорость порт 1: {0}", speed)));
//        //                        break;
//        //                }
//        //                break;
//        //            }
//        //    }
//        //    if (len == buffer.Count()) return new byte[] { };
//        //    return GetPackage(buffer.Skip(len));
//        //}


//        private dynamic GetInputPort(BaseNode sender)
//        {
//            dynamic answer = new ExpandoObject();
//            answer.success = true;
//            answer.error = string.Empty;

//            if (sender == null)
//            {
//                answer.success = false;
//                return answer;
//            }

//            var relation = NodeManager.Instance.GetInputRelations(this.Id).First(r => r.Start == sender);
//            if (relation == null)
//            {
//                answer.success = false;
//                answer.error = "запрашиваемая входящая связь отсутствует";
//                return answer;
//            }

//            if (!relation.Arg.ContainsKey("port"))
//            {
//                answer.success = false;
//                answer.error = "связь не содержит сведения о порте (отстутствует поле 'port')";
//                return answer;
//            }
//            answer.body = relation.Arg["port"];
//            return answer;
//        }

//        //protected override dynamic TaskBuild(dynamic task)
//        //{
//        //    switch ((task.what as string).ToLower())
//        //    {
//        //        //case "in-bytes":
//        //        //    {
//        //        //        //var bytes = (byte[])task.arg.bytes;
//        //        //        //var request = new BytesRequest(NetworkAddress, GetInputPort(task.arg.sender), bytes);
//        //        //        //// DoForOneOutput("in-bytes", new Dictionary<string, object> { { "bytes", request.GetBytes() } });
//        //        //        break;
//        //        //    }
//        //        //case "in-bytes-im2300":
//        //        //    {
//        //        //        //var bytes = (byte[])task.arg.bytes;
//        //        //        //var request = new Im2300BytesRequest(NetworkAddress, GetInputPort(task.arg.sender), bytes);
//        //        //        //// DoForOneOutput("in-bytes", new Dictionary<string, object> { { "bytes", request.GetBytes() } });
//        //        //        break;
//        //        //    }
//        //        //case "out-bytes":
//        //        //    {
//        //        //        var bytes = (byte[])args["bytes"];
//        //        //        packageBuffer.AddRange(bytes);
//        //        //        packageBuffer = GetPackage(packageBuffer).ToList();
//        //        //        break;
//        //        //    }
//        //        //case "output-ready":
//        //        //    {
//        //        //        //DoForInputs("output-ready", null);
//        //        //        Notify(new Dictionary<string, object>());
//        //        //        break;
//        //        //    }
//        //        //case "prepare":
//        //        //    {
//        //        //        return Prepare(sender);
//        //        //    }
//        //        //case "release":
//        //        //    {
//        //        //        Release(args.sender);
//        //        //        break;
//        //        //    }
//        //        //case "config":
//        //        //    {
//        //        //        DoForOneOutput("prepare", null);
//        //        //        var request = new ConfigRequest(NetworkAddress);
//        //        //        DoForOneOutput("in-bytes", new Dictionary<string, object> { { "bytes", request.GetBytes() } });
//        //        //        break;
//        //        //  }
//        //    }

//        //    return task;
//        //}

//        //private List<byte> packageBuffer = new List<byte>();
//        //private IEnumerable<byte> GetPackage(IEnumerable<byte> buffer)
//        //{
//        //    if (buffer == null) return new byte[] { };
//        //    var len = BitConverter.ToInt16(buffer.ToArray(), 1);

//        //    buffer = buffer.SkipWhile(b => b != NetworkAddress);

//        //    if (buffer.Count() < len) return buffer;

//        //    var frame = buffer.Take(len);
//        //    var response = new Response(frame.ToArray());
//        //    switch (response.Channel)
//        //    {
//        //        case 0x01:
//        //        case 0x02:
//        //            switch (response.Command)
//        //            {
//        //                case 0x01:
//        //                    isOpen = true;
//        //                    break;
//        //                case 0x03:
//        //                    {
//        //                        DoForInputsByPort(response.Channel, "out-bytes", new Dictionary<string, object> { { "bytes", response.Body } });
//        //                        break;
//        //                    }
//        //            }
//        //            break;

//        //        case 0x7f:
//        //            {
//        //                switch (response.Command)
//        //                {
//        //                    case 0x05:
//        //                        var speed = BitConverter.ToInt32(response.Body, 9);

//        //                        //   RaiseDataRecordsAppeared(MakeLog(string.Format("скорость порт 1: {0}", speed)));
//        //                        break;
//        //                }
//        //                break;
//        //            }
//        //    }
//        //    if (len == buffer.Count()) return new byte[] { };
//        //    return GetPackage(buffer.Skip(len));
//        //}



//        //public void Release(object sender)
//        //{
//        //    var command = new ClosePortRequest(NetworkAddress, GetInputPort(sender));
//        //    DoForOutputs("in-bytes", new Dictionary<string, object> { { "bytes", command.GetBytes() } });
//        //}

//        //public void DoForOutputs(string what, Dictionary<string, object> argument)
//        //{
//        //    var outputs = NodeManager.Instance.GetOutputRelations(this.Id).Select(r => r.End);
//        //    if (outputs != null && outputs.Any())
//        //    {
//        //        foreach (var output in outputs)
//        //        {
//        //            output.AddTask(what, argument);
//        //        }
//        //    }
//        //}

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="na">сетевой адрес</param>
//        /// <param name="port">порт</param>
//        /// <param name="cmd">команда</param>
//        /// <param name="data">байты передаваемых данных</param>
//        /// 
//        private byte[] MakeSwitchRequest(byte na, byte port, byte cmd, byte[] data)
//        {
//            var frame = new List<byte>();
//            frame.Add(na);

//            var len = 1 + 2 + 2 + data.Length + 1;
//            frame.Add(GetLowByte(len));
//            frame.Add(GetHighByte(len));
//            frame.Add(port);
//            frame.Add(cmd);
//            frame.AddRange(data);
//            var crc = CrcCalculate(frame);
//            frame.Add(crc);
//            return frame.ToArray();
//        }
//        private byte[] MakeOpenPortRequest(byte na, byte port)
//        {
//            return MakeSwitchRequest(na, port, 0x01, new byte[] { });
//        }
//        private byte[] MakeClosePortRequest(byte na, byte port)
//        {
//            return MakeSwitchRequest(na, port, 0x00, new byte[] { });
//        }
//        private byte[] MakeIm2300BytesRequest(byte na, byte port, byte[] data)
//        {
//            return MakeSwitchRequest(na, port, 0x10, data);
//        }

//        private dynamic ParseSwitchResponse(byte[] data)
//        {
//            dynamic answer = new ExpandoObject();
//            answer.success = true;
//            answer.error = string.Empty;

//            if (data == null || !data.Any() || data.Count() < 4)
//            {
//                answer.success = false;
//                answer.error = "получено не достаточно данных для обработки";
//                return answer;
//            }
//            if (!CrcCheck(data))
//            {
//                answer.success = false;
//                answer.error = "не сошлась контрольная сумма";
//                return answer;
//            }

//            answer.body = new ExpandoObject();
//            answer.body.networkAdress = data[0];
//            answer.body.port = data[3];
//            answer.body.command = data[4];
//            answer.body.data = data.Skip(5).Take(data.Count() - 5 - 1).ToArray();

//            return answer;
//        }

//        public byte GetLowByte(int b)
//        {
//            return (byte)(b & 0xFF);
//        }

//        public byte GetHighByte(int b)
//        {
//            return (byte)((b >> 8) & 0xFF);
//        }
//    }
//}
