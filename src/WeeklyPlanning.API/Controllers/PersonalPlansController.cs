using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Services;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/personal-plans")]
[Produces("application/json")]
public class PersonalPlansController : ControllerBase
{
    private readonly PersonalPlanService _service;

    public PersonalPlansController(PersonalPlanService service)
    {
        _service = service;
    }

    /// <summary>Obtiene el plan personal de un owner para una semana específica.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PersonalWeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetByOwnerAndWeek(
        [FromQuery] string ownerId,
        [FromQuery] DateOnly weekStartDate,
        CancellationToken cancellationToken)
    {
        var dto = await _service.GetByOwnerAndWeekAsync(ownerId, weekStartDate, cancellationToken);
        return dto is null ? NoContent() : Ok(dto);
    }

    /// <summary>Crea un nuevo plan personal.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PersonalWeeklyPlanDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePersonalPlanRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByOwnerAndWeek),
            new { ownerId = dto.OwnerId, weekStartDate = dto.WeekStartDate }, dto);
    }

    /// <summary>Agrega un ítem al plan.</summary>
    [HttpPost("{planId:guid}/items")]
    [ProducesResponseType(typeof(PersonalWeeklyPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        Guid planId,
        [FromBody] CreatePersonalItemRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _service.AddItemAsync(planId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Actualiza un ítem existente.</summary>
    [HttpPut("{planId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(PersonalWeeklyPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        Guid planId,
        Guid itemId,
        [FromBody] UpdatePersonalItemRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _service.UpdateItemAsync(planId, itemId, request, cancellationToken);
        return Ok(dto);
    }

    /// <summary>Elimina un ítem del plan.</summary>
    [HttpDelete("{planId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(
        Guid planId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        await _service.DeleteItemAsync(planId, itemId, cancellationToken);
        return NoContent();
    }

    /// <summary>Envía un ítem del plan personal a Chrobi.</summary>
    [HttpPost("{planId:guid}/items/{itemId:guid}/send-to-external")]
    [ProducesResponseType(typeof(SendTaskToExternalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SendTaskToExternalResultDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SendTaskToExternalResultDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SendTaskToExternalResultDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendToExternal(Guid planId, Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _service.SendItemToExternalAsync(planId, itemId, cancellationToken);
        var dto = MapSendResult(result);

        if (result.Success)
            return Ok(dto);

        if (!result.IsExternalError && result.HttpStatusCode is StatusCodes.Status400BadRequest)
            return BadRequest(dto);

        if (!result.IsExternalError && result.HttpStatusCode is StatusCodes.Status404NotFound)
            return NotFound(dto);

        return StatusCode(StatusCodes.Status502BadGateway, dto);
    }

    [HttpPost("{planId:guid}/publish-to-external")]
    [ProducesResponseType(typeof(PublishToExternalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PublishToExternalResultDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishWeekToExternal(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _service.PublishWeekToExternalAsync(planId, cancellationToken);
        var results = result.Items.Select(x => new SendItemResultDto(
            x.PersonalPlanItemId,
            x.Title,
            x.Success,
            x.ExternalTaskId,
            x.ExternalTaskUrl,
            x.ErrorMessage)).ToList();

        var dto = new PublishToExternalResultDto(
            results.Count,
            results.Count(x => x.Success),
            results.Count(x => !x.Success),
            results);

        if (!result.Success && result.Items.Count == 0)
            return NotFound(dto);

        return Ok(dto);
    }

    private static SendTaskToExternalResultDto MapSendResult(SendTaskToExternalResult result) =>
        new(result.Success, result.ExternalTaskId, result.ExternalTaskUrl, result.ErrorMessage, result.HttpStatusCode);
}

public record SendTaskToExternalResultDto(
    bool Success,
    string? ExternalTaskId,
    string? ExternalTaskUrl,
    string? ErrorMessage,
    int? HttpStatusCode);

public record PublishToExternalResultDto(
    int TotalItems,
    int Succeeded,
    int Failed,
    IReadOnlyList<SendItemResultDto> Results);

public record SendItemResultDto(
    Guid ItemId,
    string Title,
    bool Success,
    string? ExternalTaskId,
    string? ExternalTaskUrl,
    string? ErrorMessage);
