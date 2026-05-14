using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Application.Services;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.ExternalSystem;
using WeeklyPlanning.Infrastructure.Persistence;
using WeeklyPlanning.Infrastructure.Repositories;

namespace WeeklyPlanning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration? configuration = null)
    {
        services.AddDbContext<WeeklyPlanningDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));

        services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IExternalTaskSnapshotRepository, ExternalTaskSnapshotRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IPersonalPlanRepository, PersonalPlanRepository>();

        services.AddScoped<IWeeklyPlanService, WeeklyPlanService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<PersonalPlanService>();

        // Singleton token manager — shared across all HttpClients so refresh is coordinated
        services.AddSingleton<ChobiTokenService>();
        services.AddTransient<ChobiAuthHandler>();

        // HttpClient for WRITING tasks to Chrobi
        services.AddHttpClient<IExternalTaskCreationService, ExternalTaskCreationService>(
            (sp, client) =>
            {
                var config = configuration ?? sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["ExternalApi:BaseUrl"]?.TrimEnd('/') + "/"
                    ?? throw new InvalidOperationException("ExternalApi:BaseUrl is required.");
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                var t = int.TryParse(config["ExternalApi:TimeoutSeconds"], out var sec) ? sec : 30;
                client.Timeout = TimeSpan.FromSeconds(t);
            })
            .AddHttpMessageHandler<ChobiAuthHandler>();

        // HttpClient for READING from Chrobi (users, projects)
        services.AddHttpClient<IChobiReadService, ChobiReadService>(
            (sp, client) =>
            {
                var config = configuration ?? sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["ExternalApi:BaseUrl"]?.TrimEnd('/') + "/"
                    ?? throw new InvalidOperationException("ExternalApi:BaseUrl is required.");
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ChobiAuthHandler>();

        return services;
    }
}
