using Topshelf;

namespace magick_batch
{
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(configure =>
            {
                // Configure the service.
                configure.Service<Service>(service =>
                {
                    service.ConstructUsing(s => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                // Setup Account that the window service runs as.  
                configure.RunAsLocalSystem();
                configure.SetServiceName("Magick-Batch");
                configure.SetDisplayName("Magick-Batch");
            });
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            ConfigureService.Configure();
        }
    }
}
