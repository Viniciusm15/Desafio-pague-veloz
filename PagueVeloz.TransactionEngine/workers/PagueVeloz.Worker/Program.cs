using PagueVeloz.Application;
using PagueVeloz.Infrastructure;
using PagueVeloz.Infrastructure.Observability.Logging;
using PagueVeloz.Worker;

var host = Host.CreateDefaultBuilder(args)
    .UseCustomSerilog("PagueVeloz.Worker")
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddApplication();
        services.AddHostedService<OutboxProcessorWorker>();
    })
    .Build();

await host.RunAsync();
