using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.TaskManager
{
    /// <summary>
    /// блочит ноды которые сейчас могут учавствовать в опросе
    /// запоминает какие таски пытались захватить залоченную ноду, чтобы потом их уведомить об освобождении
    /// </summary>
    class NodeLocker
    {
        private readonly Dictionary<Guid, HashSet<Guid>> locks = new Dictionary<Guid, HashSet<Guid>>();

        /// <summary>
        /// отпускает ноды, используется при завершении работы маршрута
        /// </summary>
        /// <param name="ids">перечень нодов из маршрута</param>
        /// <returns>возвращает таски, которые тоже пытались залочить эти ноды</returns>
        public IEnumerable<Guid> Unlock(IEnumerable<Guid> ids)
        {
            var result = new List<Guid>();
            foreach (var id in ids)
            {
                if (locks.ContainsKey(id))
                {
                    result.AddRange(locks[id]);
                    locks.Remove(id);
                }
            }
            return result.Distinct();
        }

        public void Lock(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                if (!locks.ContainsKey(id))
                {
                    locks.Add(id, new HashSet<Guid>());
                }
            }
        }

        /// <summary>
        /// проверяет не залочены ли ноды из маршрута
        /// </summary>
        /// <param name="target">таск который проверяет ноды</param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public bool IsLock(Guid target, IEnumerable<Guid> ids)
        {
            var result = false;
            foreach (var id in ids)
            {
                if (locks.ContainsKey(id))
                {
                    if (!locks[id].Contains(target)) locks[id].Add(target);
                    result = true;
                }
            }

            return result;
        }
    }
}
