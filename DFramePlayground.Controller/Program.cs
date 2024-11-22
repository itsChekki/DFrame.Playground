using DFrame;
using Microsoft.Extensions.Hosting;

var builder = DFrameApp.CreateBuilder(7312, 7313);

builder.WorkerBuilder.UseOrleansClient(silo => silo.UseLocalhostClustering());

await builder.RunControllerAsync();