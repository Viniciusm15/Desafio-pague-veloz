using PagueVeloz.Application;
using PagueVeloz.Infrastructure;
using PagueVeloz.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHostedService<OutboxProcessorWorker>();

var host = builder.Build();
host.Run();
