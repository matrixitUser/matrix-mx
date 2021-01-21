using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using SuperSocket.SocketBase;
using Microsoft.Practices.ServiceLocation;

namespace Matrix.MatrixControllers
{
    public class MatrixSession : AppSession<MatrixSession, MatrixRequest>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Imei { get; private set; }
        public Guid Id { get; private set; }

        public override void Initialize(IAppServer<MatrixSession, MatrixRequest> appServer, ISocketSession socketSession)
        {
            try
            {
                var ss = ServiceLocator.Current.GetInstance<MatrixSocketServer>();

                var buffer = new byte[1024];
                var readed = socketSession.Client.Receive(buffer);
                var raw = Encoding.UTF8.GetString(buffer, 0, readed);

                var regex = new Regex(@"\*(?<imei>\d{15})#");
                var match = regex.Match(raw);
                if (!match.Success)
                {
                    logger.Warn("строка инициализации модема не соответствует ожидаемому формату, imei: {0}", raw);
                    //this.Close(CloseReason.ProtocolError);                
                }

                Imei = match.Groups["imei"].Value;

                var old = ss.GetSessions(s => s.Imei == Imei);
                foreach (var ses in old)
                {
                    logger.Warn("повторный выход на связь {0}, закрытие старого соединения", Imei);
                    ses.Close(CloseReason.ProtocolError);
                }

                base.Initialize(appServer, socketSession);
                CurrentState = new IdleState();

                //1 check node
                CheckForDB();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ошибка при приеме соединения");
            }
        }

        public void SendFrame(byte code, byte[] data)
        {
            ////
            byte[] frame = new byte[data.Length + 4];
            frame[0] = (byte)(frame.Count());
            frame[1] = code;

            //содержимое (со 2 по предпоследний байты)
            Array.Copy(data, 0, frame, 2, data.Length);

            //контрольная сумма
            var reverse = code != 1 && code != 2;
            var crc = Crc.Calc(frame.Take(frame.Length - 2).ToArray(), 0, frame.Length - 2);
            frame[frame.Length - 2] = crc[0];
            frame[frame.Length - 1] = crc[1];

            Send(frame, 0, frame.Length);
        }

        private void CheckForDB()
        {
            try
            {
                var url = ConfigurationManager.AppSettings["neo4j-url"];
                var client = new Neo4jClient.GraphClient(rootUri: new Uri(url));
                client.Connect();
                var query = client.Cypher.Match("(m:MatrixConnection {imei:{imei}})").WithParams(new { imei = Imei }).Return(m => m.Node<string>());
                var res = query.Results;

                if (!res.Any())
                {
                    logger.Info("новый контроллер, imei: {0}", Imei);
                }
                else
                {
                    var node = res.First().ToDynamic();
                    Id = Guid.Parse(node.id);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ошибка при поиске имея базе");
            }
        }

        public IActionState CurrentState { get; private set; }
        public bool ChangeState(IActionState state)
        {
            state.Session = this;
            CurrentState = state;
            logger.Debug("состояние изменилось, новое {0}", state);
            return true;
        }
    }
}
