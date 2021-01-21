using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    public class UnityManager
    {

        public T Resolve<T>()
        {
            return container.Resolve<T>();
        }

        public void RegisterType(Type interfase, Type type)
        {
            container.RegisterType(interfase, type, new ContainerControlledLifetimeManager());
        }

        private IUnityContainer container;
        private UnityManager()
        {
            container = new UnityContainer();
            RegisterType(typeof(IConnector), typeof(ApiConnector));
        }
        private static UnityManager instance = null;
        public static UnityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UnityManager();
                }
                return instance;
            }
        }
    }
}
