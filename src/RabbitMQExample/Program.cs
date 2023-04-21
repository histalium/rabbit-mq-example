using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQExample;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<HostOptions>(
    opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(2));

builder.Services.AddOptions<RabbitMQServerOptions>().BindConfiguration("RabbitMQServer");
builder.Services.AddHostedService<ExampleSenderProgram>();
builder.Services.AddHostedService<ExampleSubscriberProgram1>();
builder.Services.AddHostedService<ExampleSubscriberProgram2>();
using var host = builder.Build();

await host.RunAsync();
