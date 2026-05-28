using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Application.Requests;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/weekly-plans")]
[Produces("application/json")]
public class WeeklyPlansController : ControllerBase
{
    private readonly IWeeklyPlanService _service;

    public WeeklyPlansController(IWeeklyPlanService service)
    {
        _service = service;
    }

    /// <summary>Obtiene todos los planes semanales.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WeeklyPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    /// <summary>Obtiene un plan semanal por su ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Busca un plan por fecha de inicio de semana (lunes, formato: yyyy-MM-dd).</summary>
    [HttpGet("by-week")]
    [ProducesResponseType(typeof(WeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByWeek([FromQuery] DateOnly weekStartDate, CancellationToken cancellationToken)
    {
        var plan = await _service.GetByWeekStartDateAsync(weekStartDate, cancellationToken);
        if (plan is null) return NotFound();
        return Ok(plan);
    }

    /// <summary>Crea un nuevo plan semanal.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WeeklyPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateWeeklyPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    /// <summary>Actualiza las notas de un plan semanal.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWeeklyPlanRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Cambia el estado de un plan (confirm | close | revert-to-draft).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(WeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWeeklyPlanStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));

    // ─── Assignments ─────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega una asignación tentativa de tarea a una persona en el plan.
    /// No imputa horas reales. La tarea referenciada pertenece al sistema externo.
    /// </summary>
    [HttpPost("{weeklyPlanId:guid}/assignments")]
    [ProducesResponseType(typeof(TaskAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddAssignment(Guid weeklyPlanId, [FromBody] AddTaskAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _service.AddAssignmentAsync(weeklyPlanId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = weeklyPlanId }, assignment);
    }

    /// <summary>Actualiza una asignación existente (título, horas planificadas, notas).</summary>
    [HttpPut("{weeklyPlanId:guid}/assignments/{assignmentId:guid}")]
    [ProducesResponseType(typeof(TaskAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAssignment(
        Guid weeklyPlanId,
        Guid assignmentId,
        [FromBody] UpdateTaskAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAssignmentAsync(weeklyPlanId, assignmentId, request, cancellationToken));

    /// <summary>Elimina una asignación del plan.</summary>
    [HttpDelete("{weeklyPlanId:guid}/assignments/{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(Guid weeklyPlanId, Guid assignmentId, CancellationToken cancellationToken)
    {
        await _service.RemoveAssignmentAsync(weeklyPlanId, assignmentId, cancellationToken);
        return NoContent();
    }

    /// <summary>Copia asignaciones de otra semana a este plan.</summary>
    [HttpPost("{targetPlanId:guid}/copy-assignments")]
    [ProducesResponseType(typeof(IEnumerable<TaskAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CopyAssignments(
        Guid targetPlanId,
        [FromBody] CopyAssignmentsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.CopyAssignmentsAsync(targetPlanId, request.SourceAssignmentIds, cancellationToken));

    /// <summary>Envía una asignación al sistema externo (Chrobi).</summary>
    [HttpPost("{weeklyPlanId:guid}/assignments/{assignmentId:guid}/send-to-external")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendAssignmentToExternal(
        Guid weeklyPlanId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _service.SendAssignmentToExternalAsync(weeklyPlanId, assignmentId, cancellationToken);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage, statusCode = result.HttpStatusCode });
        return Ok(new { externalTaskId = result.ExternalTaskId, externalTaskUrl = result.ExternalTaskUrl });
    }

    // ─── Capacity ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el resumen de capacidad semanal por persona con advertencias si se supera la capacidad.
    /// Las advertencias son informativas; no bloquean la planificación.
    /// </summary>
    [HttpGet("{weeklyPlanId:guid}/capacity")]
    [ProducesResponseType(typeof(CapacitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapacitySummary(Guid weeklyPlanId, CancellationToken cancellationToken) =>
        Ok(await _service.GetCapacitySummaryAsync(weeklyPlanId, cancellationToken));
}
