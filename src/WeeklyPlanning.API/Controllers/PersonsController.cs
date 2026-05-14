using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Application.Requests;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Route("api/persons")]
[Produces("application/json")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _service;

    public PersonsController(IPersonService service)
    {
        _service = service;
    }

    /// <summary>Obtiene todas las personas activas del equipo.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllActiveAsync(cancellationToken));

    /// <summary>Obtiene una persona por su ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra una nueva persona en el equipo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreatePersonRequest request, CancellationToken cancellationToken)
    {
        var person = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
    }

    /// <summary>Actualiza los datos de una persona.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Asigna el ID de usuario de Chrobi a una persona del equipo.</summary>
    [HttpPatch("{id:guid}/chobi-user-id")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetChobiUserId(Guid id, [FromBody] SetChobiUserIdRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SetChobiUserIdAsync(id, request.ChobiUserId, cancellationToken));

    /// <summary>Desactiva una persona (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}

public record SetChobiUserIdRequest(int ChobiUserId);
