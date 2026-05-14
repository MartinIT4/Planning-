using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeeklyPlanning.Functions.Services;
using WeeklyPlanning.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Registra DbContext, repositorios y servicios de aplicación
        services.AddInfrastructure(connectionString);

        // Cliente HTTP hacia el sistema externo (solo lectura)
        services.AddHttpClient<ExternalApiClient>((sp, client) =>
        {
            var baseUrl = config["ExternalApi:BaseUrl"]
                ?? throw new InvalidOperationException("'ExternalApi:BaseUrl' is required.");

            var apiKey = config["ExternalApi:ApiKey"]
                ?? throw new InvalidOperationException("'ExternalApi:ApiKey' is required.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var timeoutSeconds = int.TryParse(config["ExternalApi:TimeoutSeconds"], out var t) ? t : 30;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        services.AddScoped<TaskSyncService>();
    })
    .Build();

await host.RunAsync();
