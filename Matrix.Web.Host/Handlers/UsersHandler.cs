using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using log4net;

namespace Matrix.Web.Host.Handlers
{
    class UsersHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UsersHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("user");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid userId = Guid.Parse(session.userId);

            if (what == "users-get-rights")
            {
                var answer = Helper.BuildMessage(what);
                //загрузка дерева с правами
                Guid targetId = Guid.Parse(message.body.targetId.ToString());
                //1 get tree+relations?
                answer.body.groups = StructureGraph.Instance.GetGroups(userId);
                answer.body.rights = StructureGraph.Instance.GetRightRelations(userId, targetId);
                return answer;
            }

            if (what == "users-get")
            {
                var answer = Helper.BuildMessage(what);
                //загрузка дерева с правами                
                answer.body.groups = StructureGraph.Instance.GetGroups(userId);
                return answer;
            }
            if (what == "users-password")
            {
                var answer = Helper.BuildMessage(what);

                answer.body.success = false;
                if ((string)message.body.password == "X7]JSAm5pxKoiLMzMOr0C$5K")
                {
                    answer.body.success = true;
                }

                return answer;
            }
            if (what == "users-by-login-password") // просто проверка логина и пароля
            {
                string login = message.body.login;
                string passwordHash = message.body.password;
                var user = StructureGraph.Instance.GetUser(login, passwordHash);
                if (user == null)
                {
                    var ans = Helper.BuildMessage("users-error");
                    ans.body.message = "неверный логин или пароль";
                    ans.body.success = false;
                    return ans;
                }
                var localUserId = Guid.Parse((string)user.id);

                var ansSuccess = Helper.BuildMessage("users-success");
                ansSuccess.body.user = localUserId;
                ansSuccess.body.success = true;
                return ansSuccess;
            }
            return Helper.BuildMessage(what);
        }
    }
}
