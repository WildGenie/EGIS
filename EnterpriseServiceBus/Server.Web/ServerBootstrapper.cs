using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Nancy.ViewEngines;

namespace GeoDecisions.Esb.Server.Web
{
    public class ServerBootstrapper : DefaultNancyBootstrapper
    {
        // http://simoncropp.com/embeddingviewsasresourcesinnancyfx
        // START uncomment this block of code to enable embedded views!
        //protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        //{
        //    base.ConfigureApplicationContainer(container);
        //    //This should be the assembly your views are embedded in
        //    var assembly = GetType().Assembly;
        //    ResourceViewLocationProvider
        //        .RootNamespaces
        //        //TODO: replace NancyEmbeddedViews.MyViews with your resource prefix
        //        .Add(assembly, "NancyEmbeddedViews.MyViews");
        //}

        //protected override NancyInternalConfiguration InternalConfiguration
        //{
        //    get
        //    {
        //        return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
        //    }
        //}

        //void OnConfigurationBuilder(NancyInternalConfiguration x)
        //{
        //    x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
        //}
        // END uncomment this block of code to enable embedded views!

        protected override void ConfigureConventions(NancyConventions conventions)
        {
            base.ConfigureConventions(conventions);

            conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("Content")//, @"contentFolder/subFolder")
            );

            conventions.StaticContentsConventions.Add(
              StaticContentConventionBuilder.AddDirectory("Scripts")//, @"contentFolder/subFolder")
          );

            conventions.StaticContentsConventions.Add(
              StaticContentConventionBuilder.AddDirectory("static", @"views")
          );
        }


        /// <summary>
        /// Register only NancyModules found in this assembly
        /// </summary>
        protected override IEnumerable<ModuleRegistration> Modules
        {
            get { return new[] { new ModuleRegistration(typeof(EsbModule)) }; }
        }
    }
}
