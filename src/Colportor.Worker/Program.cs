using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Colportor.Api.Data;
using Colportor.Api.Services;
using Colportor.Worker.Services;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true)
           .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(ctx.Configuration.GetConnectionString("Default")));
        services.AddSingleton<EmailService>();
        services.AddHostedService<NotifierService>();
    })
    .Build();

await host.RunAsync();
