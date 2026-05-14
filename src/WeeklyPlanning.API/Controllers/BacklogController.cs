using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Route("api/backlog")]
[Produces("application/json")]
public class BacklogController : ControllerBase
{
    private readonly IExternalTaskSnapshotRepository _repository;
    private readonly WeeklyPlanningDbContext _dbContext;

    public BacklogController(IExternalTaskSnapshotRepository repository, WeeklyPlanningDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    /// <summary>Retorna todos los snapshots de tareas externas activas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BacklogItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var snapshots = await _repository.GetAllActiveAsync(cancellationToken);
        return Ok(snapshots.Select(MapToDto));
    }

    /// <summary>Crea una tarea en el backlog manualmente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateBacklogItemRequest req, CancellationToken cancellationToken)
    {
        var externalId = $"MANUAL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var snapshot = ExternalTaskSnapshot.Create(
            externalId, req.Title, req.Description,
            req.ProjectId, req.ProjectName,
            req.EstimatedHours, 0m, "todo", null, null);

        await _repository.AddAsync(snapshot, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), MapToDto(snapshot));
    }

    /// <summary>Actualiza una tarea del backlog.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BacklogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBacklogItemRequest req, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.GetByIdAsync(id, cancellationToken);
        if (snapshot is null) return NotFound();

        snapshot.Update(
            req.Title, req.Description,
            req.ProjectId, req.ProjectName,
            req.EstimatedHours, snapshot.LoggedHours,
            snapshot.Status, snapshot.AssigneeExternalId, snapshot.AssigneeName);

        await _repository.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(snapshot));
    }

    /// <summary>Elimina una tarea del backlog.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.GetByIdAsync(id, cancellationToken);
        if (snapshot is null) return NotFound();

        snapshot.MarkInactive();
        await _repository.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Crea tareas de prueba para desarrollo local.</summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            ExternalTaskSnapshot.Create("EXT-001", "Implementar autenticación OAuth2", "Login con Google y GitHub para la app web", "PROJ-A", "Portal Web", 16m, 0m, "todo", null, null),
            ExternalTaskSnapshot.Create("EXT-002", "Refactor módulo de pagos", "Migrar a nueva librería de pagos v3", "PROJ-A", "Portal Web", 8m, 2m, "in_progress", null, "Ana García"),
            ExternalTaskSnapshot.Create("EXT-003", "Corregir bug #123 en reportes", "Los totales no cuadran cuando hay descuentos", "PROJ-B", "Backoffice", 4m, 0m, "todo", null, null),
            ExternalTaskSnapshot.Create("EXT-004", "Deploy staging environment", "Configurar pipeline CI/CD en Azure DevOps", "PROJ-B", "Backoffice", 8m, 0m, "todo", null, "Juan Pérez"),
            ExternalTaskSnapshot.Create("EXT-005", "Diseño UX pantalla de checkout", "Wireframes y prototipo en Figma", "PROJ-C", "App Mobile", 12m, 4m, "in_progress", null, null),
            ExternalTaskSnapshot.Create("EXT-006", "Optimizar queries de dashboard", "Algunas queries tardan más de 5s", "PROJ-A", "Portal Web", 6m, 0m, "todo", null, null),
            ExternalTaskSnapshot.Create("EXT-007", "Documentar API REST v2", "Swagger + ejemplos de uso para partners", "PROJ-D", "Integraciones", 10m, 0m, "todo", null, null),
            ExternalTaskSnapshot.Create("EXT-008", "Pruebas de carga endpoint /checkout", "Simular 1000 usuarios concurrentes", "PROJ-C", "App Mobile", 6m, 0m, "todo", null, null),
        };

        await _repository.AddRangeAsync(seeds, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Se crearon {seeds.Length} tareas de prueba." });
    }

    private static BacklogItemDto MapToDto(ExternalTaskSnapshot s) => new(
        s.Id, s.ExternalTaskId, s.Title, s.Description,
        s.ProjectId, s.ProjectName, s.EstimatedHours,
        s.RemainingHours, s.Status, s.AssigneeName, s.LastSyncedAt
    );
}

public record BacklogItemDto(
    Guid Id, string ExternalTaskId, string Title, string? Description,
    string? ProjectId, string? ProjectName, decimal EstimatedHours,
    decimal RemainingHours, string Status, string? AssigneeName, DateTime LastSyncedAt
);

public record CreateBacklogItemRequest(
    string Title,
    string? Description,
    string? ProjectId,
    string? ProjectName,
    decimal EstimatedHours
);

public record UpdateBacklogItemRequest(
    string Title,
    string? Description,
    string? ProjectId,
    string? ProjectName,
    decimal EstimatedHours
);
