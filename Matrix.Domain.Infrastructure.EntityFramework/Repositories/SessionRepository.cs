//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Domain.Entities;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class SessionRepository : BaseRepository, ISessionRepository
//    {
//        public SessionRepository(Context context)
//            : base(context)
//        {
//        }

//        public IEnumerable<Session> GetAll()
//        {
//            return context.Set<Session>().ToList();
//        }

//        public Session GetSessionByKey(Guid sessionKey)
//        {
//            return context.Set<Session>().FirstOrDefault(s => s.SessionKey == sessionKey);
//        }

//        public IEnumerable<Session> GetSessionsByUser(Guid userId)
//        {
//            return context.Set<Session>().Where(s => s.UserId == userId);
//        }

//        public IEnumerable<Session> GetSessionsByClientId(string clientId)
//        {
//            return context.Set<Session>().Where(s => s.ClientId == clientId);
//        }

//        public void Delete(Guid SessionId)
//        {
//            var local = context.Set<Session>().FirstOrDefault(s => s.SessionKey == SessionId);
//            if (local != null)
//            {
//                context.Set<Session>().Remove(local);
//            }
//        }
//        public void Delete(Session session)
//        {
//            if(session==null)return;
//            Delete(session.SessionKey);
//        }

//        public void Save(Session session)
//        {
//            if (session == null) return;
//            var local = context.Set<Session>().FirstOrDefault(s => s.SessionKey == session.SessionKey);
//            if (local == null)
//            {
//                context.Set<Session>().Add(session);
//            }
//        }
//    }
//}
