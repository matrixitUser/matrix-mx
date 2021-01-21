//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Matrix.Common.Agreements;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Authorize
//{
//    public static class AccessHelper
//    {
//        internal static IEnumerable<IEntity> GetAllowObjects(Group group, BaseProxy proxy)
//        {
//            if (group == null || proxy == null)
//                return new List<IEntity>();

//            var result = new List<IEntity>();

//            //var wholeCache = proxy.GetCache().ToList();

//            //var groupes = new List<Group>();
//            //var users = new List<User>();

//            //foreach (var model in wholeCache)
//            //{
//            //    if (model is User)
//            //    {
//            //        users.Add(model as User);
//            //        continue;
//            //    }
//            //    if (model is Group)
//            //    {
//            //        groupes.Add(model as Group);
//            //        continue;
//            //    }

//            //    if (!CanRead(model, group)) continue;

//            //    model.CanWrite = CanWrite(model, group);
//            //    result.Add(model);
//            //}
//            //result.AddRange(GetChildGroupAndUsers(group, groupes, users));

//            return result;
//        }

//        public static bool CanRead(IEntity cached, Group group)
//        {
//            //if (cached == null || group == null) return false;
//            //if (IsAdminGroup(group)) return true;
//            //return ContainCode(cached.AclRead, group.Code) || CanWrite(cached, group);
//            return true;
//        }
//        public static bool CanWrite(IEntity cached, Group group)
//        {
//            //if (cached == null || group == null) return false;
//            //if (IsAdminGroup(group)) return true;
//            //return ContainCode(cached.AclWrite, group.Code);
//            return true;
//        }

//        /// <summary>
//        /// Перезаписывает acl объекта на чтение кодами тех групп, которые переданы
//        /// </summary>
//        /// <param name="cached"></param>
//        /// <param name="groups"></param>
//        public static void SetRead(IEntity cached, IEnumerable<Group> groups)
//        {
//            if (cached == null) return;

//            //cached.AclRead = GetAcl(groups);
//        }
//        /// <summary>
//        /// Перезаписывает acl объекта на запись кодами тех групп, которые переданы
//        /// </summary>
//        /// <param name="cached"></param>
//        /// <param name="groups"></param>
//        public static void SetWrite(IEntity cached, IEnumerable<Group> groups)
//        {
//            if (cached == null) return;

//            //cached.AclWrite = GetAcl(groups);
//        }

//        public static EditInfo CanAcceptChanges(IEntity old, IEntity edited, Group group)
//        {
//            //if (!CanWrite(old, group))
//            //{
//            //    return new EditInfo { IsSuccess = false, ErrorMessage = "Нет доступа для редактирования объекта" };
//            //}

//            //var type = old.GetType();

//            //if (type != edited.GetType()) return new EditInfo { IsSuccess = false };

//            //PropertyInfo[] properties = type.GetProperties();

//            //foreach (var propertyInfo in properties)
//            //{
//            //    var attribute = propertyInfo.GetCustomAttributes(typeof(UneditableAttribute), true).FirstOrDefault();
//            //    if (attribute == null) continue;

//            //    var oldValue = propertyInfo.GetValue(old, null);
//            //    var editedValue = propertyInfo.GetValue(edited, null);
//            //    if (!oldValue.Equals(editedValue))
//            //        return new EditInfo
//            //                   {
//            //                       IsSuccess = false,
//            //                       ErrorMessage = "Попытка отредактировать нередактируемое поле",
//            //                   };
//            //}
//            return new EditInfo { IsSuccess = true };
//        }
//        #region HelpMethod
//        internal static IEnumerable<IEntity> GetChildGroupAndUsers(Group group, IEnumerable<Group> groups, IEnumerable<User> users)
//        {
//            //if (group == null || groups == null || users == null) return new List<AggregationRoot>();


//            //var childGroups = GetChildGroups(group, groups).ToList();
//            //childGroups.Add(group);
//            //var childUsers = users.Where(u => childGroups.Any(g => g.Id == u.GroupId)).ToList();
//            //var result = new List<AggregationRoot>();
//            //result.AddRange(childGroups);
//            //result.AddRange(childUsers);
//            //return result;
//            return new IEntity[] { };
//        }
//        private static IEnumerable<Group> GetChildGroups(Group group, IEnumerable<Group> groups)
//        {
//            var result = new List<Group>();
//            foreach (var currentGroup in groups)
//            {
//                if (currentGroup.ParentId != group.Id) continue;

//                result.Add(currentGroup);
//                result.AddRange(GetChildGroups(currentGroup, groups));
//            }
//            return result;
//        }
//        public static bool IsAdminGroup(Group group)
//        {
//            if (group == null) return false;
//            return group.ParentId == null;
//        }
//        private static bool ContainCode(string acl, string code)
//        {
//            if (string.IsNullOrEmpty(acl)) return false;
//            if (string.IsNullOrEmpty(code)) return false;

//            var aclMas = acl.Split(';');
//            foreach (var ace in aclMas)
//            {
//                if (ace == code) return true;
//                if (ace.Contains(string.Format("{0}.", code))) return true;
//            }
//            return false;
//        }
//        public static string GetAcl(IEnumerable<Group> groups)
//        {
//            var result = string.Empty;

//            if (groups != null)
//            {
//                foreach (var gr in groups)
//                {
//                    result += string.Format("{0};", gr.Code);
//                }
//            }

//            return result;
//        }

//        public static void CopyReadAcl(IEntity source, IEntity target)
//        {
//            if (target == null || source == null) return;

//            source.AclRead = target.AclRead;
//        }
//        public static void CopyWriteAcl(IEntity source, IEntity target)
//        {
//            if (target == null || source == null) return;

//            source.AclWrite = target.AclWrite;
//        }
//        /// <summary>
//        /// Добавляет в ACL объекта <param name="source"></param> только те группы, которых у него нет, но
//        /// они есть в в объекте <param name="target"></param>
//        /// </summary>
//        /// <param name="target"></param>
//        /// <param name="source"></param>
//        public static void AddGroupReadAcl(IEntity group, IEntity target)
//        {
//            //if (target == null || group == null || string.IsNullOrEmpty(group.AclWrite)) return;
//            //if (string.IsNullOrEmpty(group.AclRead))
//            //    group.AclRead = string.Empty;

//            //string[] targetGroups = target.AclRead == null ? new string[] { } : target.AclRead.Split(';');
//            //string[] sourceGroups = group.AclRead.Split(';');
//            //foreach (var sourceGroup in sourceGroups)
//            //{
//            //    if (string.IsNullOrEmpty(sourceGroup) || string.IsNullOrWhiteSpace(sourceGroup)) continue;

//            //    if (!targetGroups.Contains(sourceGroup))
//            //    {
//            //        target.AclRead += sourceGroup + ";";
//            //    }
//            //}
//        }

//        /// <summary>
//        /// Добавляет в ACL объекта <param name="group"></param> только те группы, которых у него нет, но
//        /// они есть в в объекте <param name="target"></param>
//        /// </summary>
//        /// <param name="target"></param>
//        /// <param name="group"></param>
//        public static void AddGroupWriteAcl(IEntity group, IEntity target)
//        {
//            //if (target == null || group == null || string.IsNullOrEmpty(target.AclWrite)) return;
//            //if (string.IsNullOrEmpty(group.AclWrite))
//            //    group.AclWrite = string.Empty;

//            //string[] targetGroups = target.AclWrite.Split(';');
//            //string[] sourceGroups = group.AclWrite.Split(';');
//            //foreach (var targetGroup in targetGroups)
//            //{
//            //    if (string.IsNullOrEmpty(targetGroup) || string.IsNullOrWhiteSpace(targetGroup)) continue;

//            //    if (!sourceGroups.Contains(targetGroup))
//            //    {
//            //        group.AclWrite += targetGroup + ";";
//            //    }
//            //}
//        }

//        private static string ExcludeGroup(string acl, Group group)
//        {
//            if (acl == null) return null;
//            if (group == null) return acl;

//            string[] sourceGroups = acl.Split(';');
//            string result = string.Empty;

//            foreach (string sourceGroup in sourceGroups)
//            {
//                if (!sourceGroup.StartsWith(group.Code))
//                {
//                    result += sourceGroup + ";";
//                }
//            }
//            return result;
//        }
//        public static void ExcludeWriteGroup(IEntity source, Group group)
//        {
//            //if (source == null) return;

//            //source.AclWrite = ExcludeGroup(source.AclWrite, group);
//        }
//        public static void ExcludeReadGroup(IEntity source, Group group)
//        {
//            //if (source == null) return;

//            //source.AclRead = ExcludeGroup(source.AclRead, group);
//        }
//        #endregion

//        public static void IncludeReadGroup(IEntity source, Group group)
//        {
//            //if (source == null)
//            //    return;
//            //source.AclRead = IncludeGroup(source.AclRead, group);
//        }
//        public static void IncludeWriteGroup(IEntity source, Group group)
//        {
//            //if (source == null)
//            //    return;
//            //source.AclWrite = IncludeGroup(source.AclWrite, group);
//        }
//        private static string IncludeGroup(string acl, Group group)
//        {
//            if (acl == null) acl = string.Empty;
//            if (group == null) return acl;

//            string[] sourceGroups = acl.Split(';');

//            foreach (string sourceGroup in sourceGroups)
//            {
//                if (sourceGroup.StartsWith(group.Code))
//                {
//                    return acl;
//                }
//            }
//            return acl + group.Code + ";";

//        }
//    }
//}
