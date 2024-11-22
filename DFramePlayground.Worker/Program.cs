using DFrame;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using StackExchange.Redis;

var host = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();
    
    await host.RunDFrameWorkerAsync("http://localhost:7313");