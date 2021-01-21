using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using NLog;

namespace Matrix.MatrixControllers
{
    [ActionHandler("idle")]
    class IdleState : IActionState
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void AcceptFrame(MatrixRequest request)
        {
            switch (request.Command)
            {
                case 24:
                    var hm = ServiceLocator.Current.GetInstance<HandlerManager>();
                    Session.ChangeState(hm.Get("alarm"));
                    break;
            }
            //parse frame here
            logger.Debug("поступил пакет {0}", string.Join(",", request.Body.Select(b => b.ToString("X2"))));
        }

        public Task<bool> Start(IEnumerable<dynamic> path, dynamic details)
        {
            return Task.Run<bool>(() =>
            {
                return true;
            });
        }

        public MatrixSession Session { get; set; }

        public bool CanChange()
        {
            return true;
        }
    }
}
