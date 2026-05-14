using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Functions.Services;

namespace WeeklyPlanning.Functions.Functions;

/// <summary>
/// Sincronización automática programada.
/// Por defecto: todos los días hábiles a las 07:00 y 13:00 UTC.
/// Configurable mediante la app setting "SyncSchedule" (expresión CRON de 6 partes).
/// 
/// Formato CRON: {segundos} {minutos} {horas} {día-mes} {mes} {día-semana}
/// Ejemplo cada hora: "0 0 * * * *"
/// Ejemplo lunes a viernes 7:00 y 13:00 UTC: "0 0 7,13 * * 1-5"
/// </summary>
public class TaskSyncTimerFunction
{
    private readonly TaskSyncService _syncService;
    private readonly ILogger<TaskSyncTimerFunction> _logger;

    public TaskSyncTimerFunction(TaskSyncService syncService, ILogger<TaskSyncTimerFunction> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [Function(nameof(TaskSyncTimerFunction))]
    public async Task Run(
        [TimerTrigger("%SyncSchedule%")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Timer sync triggered at {UtcNow} UTC. IsPastDue: {IsPastDue}.",
            DateTime.UtcNow, timer.IsPastDue);

        if (timer.IsPastDue)
            _logger.LogWarning("Timer was past due — executing missed sync run.");

        var result = await _syncService.SyncAsync(cancellationToken);

        _logger.LogInformation(
            "Timer sync result: Success={Success}, Created={Created}, Updated={Updated}, Deactivated={Deactivated}, Duration={DurationMs}ms.",
            result.Success, result.Created, result.Updated, result.Deactivated, (int)result.Duration.TotalMilliseconds);

        if (timer.ScheduleStatus is { } schedule)
            _logger.LogInformation("Next sync scheduled at {Next} UTC.", schedule.Next.ToUniversalTime());
    }
}
