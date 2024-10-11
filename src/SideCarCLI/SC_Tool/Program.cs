using Microsoft.Extensions.Logging.Abstractions;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = CreateServices();
        var watcher = services.GetRequiredService<IRunWatcherDetect>(); 
        ArgumentNullException.ThrowIfNull(watcher, nameof(watcher));
        watcher.Folder = Environment.CurrentDirectory;
        var logger= services.GetRequiredService<ILogger<Program>>();
        var versions = await watcher.DetectVersions();
        if (versions.Length == 0)
        {
            logger.LogError("No versions detected");
            throw new InvalidOperationException("No versions detected");
        }
        var lastVersion = versions.OrderByDescending(v => v.Version).First();
        logger.LogInformation($"Running version {lastVersion.Version}");
        return await lastVersion.Run(args);
        
    }
    private static ServiceProvider CreateServices()
    {
        var serviceProvider = new ServiceCollection()
            //.AddSingleton<IRunWatcher>((IRunWatcher)null)
            //.AddSingleton<ILogger>(new NullLogger())
            .BuildServiceProvider();

        return serviceProvider;
    }
}