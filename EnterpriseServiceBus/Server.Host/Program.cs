using Topshelf;

namespace Server.Host
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(configurator =>
                {
                    configurator.Service<ServerService>(s =>
                        {
                            s.ConstructUsing(name => new ServerService());
                            s.WhenStarted((service, hostControl) => service.Start(hostControl));
                            s.WhenStopped(service => service.Stop());
                            s.WhenContinued(service => service.Continue());
                            s.WhenPaused(service => service.Pause());
                        });
                    //configurator.RunAsPrompt();
                    configurator.SetDescription("GeoDecisions Enterprise Service Bus Server");
                    configurator.SetServiceName("GeoDecisions.Esb.Server");
                    configurator.StartAutomatically();
                });
        }
    }
}