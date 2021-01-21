using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Nancy.Bootstrappers.Unity;
using Nancy.Conventions;

namespace Matrix.MatrixControllers
{
    class Bootstrapper : UnityNancyBootstrapper
    {
        private IUnityContainer unityContainer;

        public Bootstrapper(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;
        }

        protected override IUnityContainer GetApplicationContainer()
        {
            return unityContainer;
        }

        protected override void ConfigureConventions(Nancy.Conventions.NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("ui", @"ui"));
        }
    }
}
