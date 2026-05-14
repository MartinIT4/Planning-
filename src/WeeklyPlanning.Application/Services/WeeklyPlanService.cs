using Microsoft.Extensions.Configuration;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Exceptions;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Application.Requests;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Exceptions;
using WeeklyPlanning.Domain.Interfaces;

namespace WeeklyPlanning.Application.Services;

public class WeeklyPlanService : IWeeklyPlanService
{
    private const int DefaultCreatorUserId = 162;

    private readonly IWeeklyPlanRepository _weeklyPlanRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IExternalTaskCreationService _externalService;
    private readonly IConfiguration _config;

    public WeeklyPlanService(
        IWeeklyPlanRepository weeklyPlanRepository,
        IPersonRepository personRepository,
        IProjectRepository projectRepository,
        IExternalTaskCreationService externalService,
        IConfiguration config)
    {
        _weeklyPlanRepository = weeklyPlanRepository;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _externalService = externalService;
        _config = config;
    }

    public async Task<WeeklyPlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), id);
        return MapToDto(plan);
    }

    public async Task<WeeklyPlanDto?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByWeekStartDateAsync(weekStartDate, cancellationToken);
        return plan is null ? null : MapToDto(plan);
    }

    public async Task<IEnumerable<WeeklyPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _weeklyPlanRepository.GetAllAsync(cancellationToken);
        return plans.Select(MapToDto);
    }

    public async Task<WeeklyPlanDto> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _weeklyPlanRepository.GetByWeekStartDateAsync(request.WeekStartDate, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Ya existe un plan para la semana del {request.WeekStartDate:yyyy-MM-dd}.");

        WeeklyPlan plan;
        try
        {
            plan = WeeklyPlan.Create(request.WeekStartDate, request.Notes);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _weeklyPlanRepository.AddAsync(plan, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<WeeklyPlanDto> UpdateAsync(Guid id, UpdateWeeklyPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), id);

        plan.UpdateNotes(request.Notes);
        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<WeeklyPlanDto> UpdateStatusAsync(Guid id, UpdateWeeklyPlanStatusRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), id);

        try
        {
            switch (request.Action.ToLowerInvariant())
            {
                case "confirm":
                    plan.Confirm();
                    break;
                case "close":
                    plan.Close();
                    break;
                case "revert-to-draft":
                    plan.RevertToDraft();
                    break;
                default:
                    throw new ValidationException($"Acción '{request.Action}' no válida. Valores permitidos: confirm, close, revert-to-draft.");
            }
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<TaskAssignmentDto> AddAssignmentAsync(Guid weeklyPlanId, AddTaskAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), weeklyPlanId);

        var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), request.PersonId);

        if (!person.IsActive)
            throw new ValidationException($"La persona '{person.Name}' no está activa.");

        TaskAssignment assignment;
        try
        {
            assignment = plan.AddAssignment(
                request.PersonId,
                request.ExternalTaskId,
                request.TaskTitle,
                request.PlannedHours,
                request.Notes);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return MapAssignmentToDto(assignment, person.Name);
    }

    public async Task<TaskAssignmentDto> UpdateAssignmentAsync(Guid weeklyPlanId, Guid assignmentId, UpdateTaskAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), weeklyPlanId);

        var assignment = plan.Assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new NotFoundException(nameof(TaskAssignment), assignmentId);

        try
        {
            assignment.Update(request.TaskTitle, request.PlannedHours, request.Notes);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);

        var person = await _personRepository.GetByIdAsync(assignment.PersonId, cancellationToken);
        return MapAssignmentToDto(assignment, person?.Name ?? "");
    }

    public async Task RemoveAssignmentAsync(Guid weeklyPlanId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), weeklyPlanId);

        try
        {
            plan.RemoveAssignment(assignmentId);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
    }

    public async Task<CapacitySummaryDto> GetCapacitySummaryAsync(Guid weeklyPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), weeklyPlanId);

        var hoursByPerson = plan.GetHoursByPerson();
        var personIds = hoursByPerson.Keys.ToList();

        var persons = await _personRepository.GetAllActiveAsync(cancellationToken);
        var personDict = persons.ToDictionary(p => p.Id);

        var capacities = personIds.Select(personId =>
        {
            personDict.TryGetValue(personId, out var person);
            var capacity = person?.WeeklyCapacityHours ?? 40m;
            var planned = hoursByPerson[personId];
            var remaining = capacity - planned;
            var isOver = planned > capacity;

            return new PersonCapacityDto(
                PersonId: personId,
                PersonName: person?.Name ?? "Desconocido",
                WeeklyCapacityHours: capacity,
                PlannedHours: planned,
                RemainingHours: remaining,
                IsOverCapacity: isOver,
                WarningMessage: isOver
                    ? $"ADVERTENCIA: {person?.Name ?? "Persona"} tiene {planned}h planificadas pero su capacidad es {capacity}h ({planned - capacity}h de exceso)."
                    : null
            );
        });

        return new CapacitySummaryDto(
            WeeklyPlanId: plan.Id,
            WeekStartDate: plan.WeekStartDate,
            WeekEndDate: plan.WeekEndDate,
            PersonCapacities: capacities
        );
    }

    public async Task<IEnumerable<TaskAssignmentDto>> CopyAssignmentsAsync(
        Guid targetPlanId,
        IEnumerable<Guid> sourceAssignmentIds,
        CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(targetPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), targetPlanId);

        var requestedIds = sourceAssignmentIds?.Distinct().ToHashSet() ?? [];
        if (requestedIds.Count == 0)
            return [];

        var allPlans = await _weeklyPlanRepository.GetAllAsync(cancellationToken);
        var sourceAssignments = allPlans
            .SelectMany(p => p.Assignments)
            .Where(a => requestedIds.Contains(a.Id))
            .ToList();

        var existingKeys = plan.Assignments
            .Select(a => $"{a.PersonId:N}|{a.ExternalTaskId}")
            .ToHashSet();

        var copied = new List<TaskAssignmentDto>();
        foreach (var assignment in sourceAssignments)
        {
            var key = $"{assignment.PersonId:N}|{assignment.ExternalTaskId}";
            if (!existingKeys.Add(key))
                continue;

            TaskAssignment newAssignment;
            try
            {
                newAssignment = plan.AddAssignment(
                    assignment.PersonId,
                    assignment.ExternalTaskId,
                    assignment.TaskTitle,
                    assignment.PlannedHours,
                    assignment.Notes);
            }
            catch (DomainException ex)
            {
                throw new ValidationException(ex.Message);
            }

            copied.Add(MapAssignmentToDto(newAssignment, assignment.Person?.Name ?? string.Empty));
        }

        if (copied.Count > 0)
            await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);

        return copied;
    }

    public async Task<SendTaskToExternalResult> SendAssignmentToExternalAsync(
        Guid weeklyPlanId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), weeklyPlanId);

        var assignment = plan.Assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new NotFoundException(nameof(TaskAssignment), assignmentId);

        // Idempotency guard: already sent, don't create duplicate in Chrobi
        if (assignment.SentToExternalAt.HasValue)
            return SendTaskToExternalResult.Ok(assignment.ExternalCreatedTaskId ?? $"already-sent-{assignmentId}");

        var person = await _personRepository.GetByIdAsync(assignment.PersonId, cancellationToken);
        if (person is null)
            return SendTaskToExternalResult.Fail("La persona asignada no existe.", 400);
        if (!person.ChobiUserId.HasValue)
            return SendTaskToExternalResult.Fail($"'{person.Name}' no tiene configurado ChobiUserId.", 400);

        // ExternalTaskId format: "PROJ-{guid}"
        int? chobiProjectId = null;
        if (assignment.ExternalTaskId.StartsWith("PROJ-", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(assignment.ExternalTaskId[5..], out var projectGuid))
        {
            var project = await _projectRepository.GetByIdAsync(projectGuid, cancellationToken);
            if (project is null)
                return SendTaskToExternalResult.Fail("El proyecto referenciado en la asignación no existe.", 400);
            chobiProjectId = project.ChobiProjectId;
        }

        if (!chobiProjectId.HasValue)
            return SendTaskToExternalResult.Fail("La asignación no tiene un proyecto Chrobi vinculado.", 400);

        var creatorUserId = int.TryParse(_config["ExternalApi:CreatorUserId"], out var cid) ? cid : DefaultCreatorUserId;

        // Use the user's custom description as the task title in Chrobi (e.g. "Gestión de proyecto"),
        // falling back to the cached project/task name only if no description was provided.
        var chobiTitle = !string.IsNullOrWhiteSpace(assignment.Notes)
            ? assignment.Notes
            : assignment.TaskTitle;

        var request = new SendTaskToExternalRequest(
            PersonalPlanItemId: assignment.Id,
            Title: chobiTitle,
            Description: null,
            EstimatedHours: assignment.PlannedHours,
            AssigneeChobiUserId: person.ChobiUserId.Value,
            ChobiProjectId: chobiProjectId,
            WeekStartDate: plan.WeekStartDate,
            WeekEndDate: plan.WeekEndDate,
            CreatorChobiUserId: creatorUserId);

        var result = await _externalService.SendAsync(request, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ExternalTaskId))
            return result;

        assignment.MarkAsSent(result.ExternalTaskId);
        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return result;
    }

    private static WeeklyPlanDto MapToDto(WeeklyPlan plan) =>
        new(
            Id: plan.Id,
            WeekStartDate: plan.WeekStartDate,
            WeekEndDate: plan.WeekEndDate,
            Status: plan.Status.ToString(),
            Notes: plan.Notes,
            Assignments: plan.Assignments.Select(a => MapAssignmentToDto(a, a.Person?.Name ?? "")),
            CreatedAt: plan.CreatedAt,
            UpdatedAt: plan.UpdatedAt
        );

    private static TaskAssignmentDto MapAssignmentToDto(TaskAssignment a, string personName) =>
        new(
            Id: a.Id,
            WeeklyPlanId: a.WeeklyPlanId,
            PersonId: a.PersonId,
            PersonName: personName,
            ExternalTaskId: a.ExternalTaskId,
            TaskTitle: a.TaskTitle,
            PlannedHours: a.PlannedHours,
            Notes: a.Notes,
            SentToExternalAt: a.SentToExternalAt,
            ExternalCreatedTaskId: a.ExternalCreatedTaskId,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt
        );
}
