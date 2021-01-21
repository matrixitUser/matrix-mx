using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Nancy.Bootstrappers.Unity;
using Nancy.Conventions;

namespace Matrix.Scheduler
{
    class NancyBootstrapper : UnityNancyBootstrapper
    {
        private IUnityContainer container;

        public NancyBootstrapper(IUnityContainer container)
        {
            this.container = container;
        }

        protected override IUnityContainer GetApplicationContainer()
        {
            return container;            
        }

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(
                 StaticContentConventionBuilder.AddDirectory("ui", @"ui")
            );
        }
    }
}
