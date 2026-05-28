using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IProjectRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <summary>Retorna todos los proyectos activos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var projects = await _repository.GetAllActiveAsync(_currentUser.OwnerId, cancellationToken);
        return Ok(projects.Select(MapToDto));
    }

    /// <summary>Crea un nuevo proyecto.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken cancellationToken)
    {
        var project = Project.Create(_currentUser.OwnerId, req.Name, req.Description, req.IsBillable);
        await _repository.AddAsync(project, cancellationToken);
        return CreatedAtAction(nameof(GetAll), MapToDto(project));
    }

    /// <summary>Actualiza un proyecto existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(id, cancellationToken);
        if (project is null || !string.Equals(project.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase)) return NotFound();

        project.Update(req.Name, req.Description, req.IsBillable);
        await _repository.UpdateAsync(project, cancellationToken);
        return Ok(MapToDto(project));
    }

    [HttpPatch("{id:guid}/chobi-project-id")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetChobiProjectId(Guid id, [FromBody] SetChobiProjectIdRequest request, CancellationToken cancellationToken)
    {
        if (request.ChobiProjectId <= 0)
            return BadRequest(new { error = "El ChobiProjectId debe ser mayor a cero." });

        var project = await _repository.GetByIdAsync(id, cancellationToken);
        if (project is null || !string.Equals(project.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase)) return NotFound();

        project.SetChobiProjectId(request.ChobiProjectId);
        await _repository.UpdateAsync(project, cancellationToken);
        return Ok(MapToDto(project));
    }

    /// <summary>Desactiva un proyecto (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(id, cancellationToken);
        if (project is null || !string.Equals(project.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase)) return NotFound();

        project.Deactivate();
        await _repository.UpdateAsync(project, cancellationToken);
        return NoContent();
    }

    private static ProjectDto MapToDto(Project p) => new(p.Id, p.Name, p.Description, p.ChobiProjectId, p.IsActive, p.IsBillable);
}

public record ProjectDto(Guid Id, string Name, string? Description, int? ChobiProjectId, bool IsActive, bool IsBillable);
public record CreateProjectRequest(string Name, string? Description, bool IsBillable = false);
public record UpdateProjectRequest(string Name, string? Description, bool IsBillable = false);
public record SetChobiProjectIdRequest(int ChobiProjectId);
