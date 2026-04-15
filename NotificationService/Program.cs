using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<MailSender>();
    })
    .Build();

await host.RunAsync();
