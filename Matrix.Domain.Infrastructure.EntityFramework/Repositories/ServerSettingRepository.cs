//using System.Collections.Generic;
//using System.Linq;
//using Matrix.Domain.Entities;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class ServerSettingRepository : BaseRepository, IServerSettingRepository
//    {
//        public ServerSettingRepository(Context context) : base(context) { }

//        public IEnumerable<ServerSetting> GetAll()
//        {
//            return context.Set<ServerSetting>();
//        }

//        public ServerSetting GetByKey(string key)
//        {
//            return context.Set<ServerSetting>().FirstOrDefault(s => s.SettingKey == key);
//        }

//        public void Save(ServerSetting serverSetting)
//        {
//            var local = context.Set<ServerSetting>().FirstOrDefault(s => s.SettingKey == serverSetting.SettingKey);
//            if (local == null)
//            {
//                context.Set<ServerSetting>().Add(serverSetting);
//            }
//            else
//            {
//                local.Value = serverSetting.Value;
//            }
//        }

//        public void Delete(ServerSetting serverSetting)
//        {
//            var local = GetByKey(serverSetting.SettingKey);
//            if (local == null) return;
//            context.Set<ServerSetting>().Remove(local);
//        }
//    }
//}
