using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;

namespace Matrix.MatrixControllers
{
    /// <summary>
    /// ищет обработчик для заданной команды
    /// </summary>
    public class HandlerManager
    {
        public IActionState Get(string what)
        {
            var type = Assembly.GetCallingAssembly().GetTypes().
                Where(t => typeof(IActionState).IsAssignableFrom(t)).
                Where(t =>
                {
                    var attr = Attribute.GetCustomAttribute(t, typeof(ActionHandlerAttribute));
                    return attr != null && (attr as ActionHandlerAttribute).What == what;
                }).FirstOrDefault();

            return (IActionState)Activator.CreateInstance(type);
        }
    }
}
