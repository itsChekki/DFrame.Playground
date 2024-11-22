using StackExchange.Redis;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .ConfigureServices(services => services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("127.0.0.1:6379")))
    .UseConsoleLifetime();
    
using var host = builder.Build();
await host.RunAsync();