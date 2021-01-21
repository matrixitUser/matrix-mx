using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    /// <summary>
    /// обработчик сообщений авторизации
    /// этот обработчик работает особым образом: 
    /// </summary>
    class AuthHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AuthHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("auth");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "auth-by-login")
            {
                //return System.Threading.Tasks.Task.Run<dynamic>(() =>
                //{
                string login = message.body.login;
#if ORENBURG
                string password = message.body.password;
                var passwordHash = GetHashString(password);
#else
                string passwordHash = message.body.password;
#endif
                var user = StructureGraph.Instance.GetUser(login, passwordHash);
                if (user == null)
                {
                    var ans = Helper.BuildMessage("auth-error");
                    ans.body.message = "неверный логин или пароль";
                    return ans;
                }

                var userId = Guid.Parse((string)user.id);
                dynamic newSession = new ExpandoObject(); //Session.Create(findedUser);
                newSession.id = Guid.NewGuid();
                newSession.bag = new ExpandoObject();
                newSession.user = user;

                Data.CacheRepository.Instance.SaveSession(newSession, userId);
                
                /*
                //рабочий код 19.02.2019 изменен из-за redis
                var findedUser = StructureGraph.Instance.GetUser(login, passwordHash);
                if (findedUser == null)
                {
                    var ans = Helper.BuildMessage("auth-error");
                    ans.body.message = "неверный логин или пароль";
                    return ans;
                }

                var userId = Guid.Parse((string)findedUser.id);
                var user = StructureGraph.Instance.GetNodeById(userId, userId);

                dynamic newSession = new ExpandoObject(); //Session.Create(findedUser);
                newSession.id = Guid.NewGuid();
                newSession.bag = new ExpandoObject();
                newSession.user = user;

                Data.CacheRepository.Instance.SaveSession(newSession, userId);

                newSession = Data.CacheRepository.Instance.GetSession(newSession.id);
                //SessionManager.Instance.AddSession(newSession);
                */
                var ansSuccess = Helper.BuildMessage("auth-success");
                ansSuccess.body.user = newSession.user;
                //ansSuccess.body.user.roles = new string[] { "Admin", "User" };
                ansSuccess.body.sessionId = newSession.id;
                return ansSuccess;
                //});

            }
            if (what == "auth-by-login1") // просто проверка логина и пароля
            {
                string login = message.body.login;
                string passwordHash = message.body.password;
                var user = StructureGraph.Instance.GetUser(login, passwordHash);
                if (user == null)
                {
                    var ans = Helper.BuildMessage("auth-error");
                    ans.body.message = "неверный логин или пароль";
                    return ans;
                }

                var userId = Guid.Parse((string)user.id);
                
                var ansSuccess = Helper.BuildMessage("auth-success");
                ansSuccess.body.user = userId;
                return ansSuccess;
            }
            if (what == "auth-by-session")
            {
                //return System.Threading.Tasks.Task.Run<dynamic>(() =>
                //    {
                Guid sessionId;
                if (message.body.sessionId.GetType() == typeof(Guid))

                    sessionId = message.body.sessionId;
                else
                    sessionId = Guid.Parse(message.body.sessionId);

                var newSession = Data.CacheRepository.Instance.GetSession(sessionId);

                //var newSession = Session.TryCreate(sessionId);
                if (newSession == null)
                {
                    var ans = Helper.BuildMessage("auth-error");
                    ans.body.message = "указанная сессия не найдена";
                    return ans;
                }
                //SessionManager.Instance.AddSession(newSession);

                var ansSuccess = Helper.BuildMessage("auth-success");

                ansSuccess.body.user = newSession.user;
                ansSuccess.body.sessionId = newSession.id;
                return ansSuccess;
                //});
            }

            if (what == "auth-close-session")
            {
                Guid sessionId = Guid.Parse((string)message.body.sessionId);
                StructureGraph.Instance.CloseSession(sessionId);
                var ans = Helper.BuildMessage("auth-session-closed");
                return ans;
            }

            return null;
        }

        public static string GetHashString(string input)
        {
            //// step 1, calculate MD5 hash from input
            //MD5 md5 = System.Security.Cryptography.MD5.Create();
            //byte[] inputBytes = System.Text.Encoding.GetEncoding(1251).GetBytes(input);

            //byte[] hash = md5.ComputeHash(inputBytes);

            //return string.Join("", hash.Select(b=>b.ToString("x2")));

            //return string.Join("", MD5CryptoServiceProvider.Create().ComputeHash(Encoding.Unicode.GetBytes(input)).Select(b => b.ToString("x2")));

            if (input == null) return null;
            MD5 md5Hash = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public bool VerifyHash(string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetHashString(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;


            return comparer.Compare(hashOfInput, hash) == 0;
        }

    }
}
