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
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IExternalTaskCreationService _externalService;
    private readonly IChobiReadService _chobiReadService;
    private readonly ICurrentUserService _currentUser;

    public WeeklyPlanService(
        IWeeklyPlanRepository weeklyPlanRepository,
        IPersonRepository personRepository,
        IProjectRepository projectRepository,
        IExternalTaskCreationService externalService,
        IChobiReadService chobiReadService,
        ICurrentUserService currentUser)
    {
        _weeklyPlanRepository = weeklyPlanRepository;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _externalService = externalService;
        _chobiReadService = chobiReadService;
        _currentUser = currentUser;
    }

    public async Task<WeeklyPlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(id, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<WeeklyPlanDto?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var plan = await _weeklyPlanRepository.GetByWeekStartDateAsync(weekStartDate, _currentUser.OwnerId, cancellationToken);
        return plan is null ? null : MapToDto(plan);
    }

    public async Task<IEnumerable<WeeklyPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _weeklyPlanRepository.GetAllAsync(_currentUser.OwnerId, cancellationToken);
        return plans.Select(MapToDto);
    }

    public async Task<WeeklyPlanDto> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _weeklyPlanRepository.GetByWeekStartDateAsync(request.WeekStartDate, _currentUser.OwnerId, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Ya existe un plan para la semana del {request.WeekStartDate:yyyy-MM-dd}.");

        WeeklyPlan plan;
        try
        {
            plan = WeeklyPlan.Create(_currentUser.OwnerId, request.WeekStartDate, request.Notes);
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
        var plan = await GetOwnedPlanAsync(id, cancellationToken);

        plan.UpdateNotes(request.Notes);
        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return MapToDto(plan);
    }

    public async Task<WeeklyPlanDto> UpdateStatusAsync(Guid id, UpdateWeeklyPlanStatusRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(id, cancellationToken);

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
        var plan = await GetOwnedPlanAsync(weeklyPlanId, cancellationToken);

        var person = await GetOwnedPersonAsync(request.PersonId, cancellationToken);

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
        var plan = await GetOwnedPlanAsync(weeklyPlanId, cancellationToken);

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

        var person = await GetOwnedPersonAsync(assignment.PersonId, cancellationToken);
        return MapAssignmentToDto(assignment, person.Name);
    }

    public async Task RemoveAssignmentAsync(Guid weeklyPlanId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(weeklyPlanId, cancellationToken);

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
        var plan = await GetOwnedPlanAsync(weeklyPlanId, cancellationToken);

        var hoursByPerson = plan.GetHoursByPerson();
        var personIds = hoursByPerson.Keys.ToList();

        var persons = await _personRepository.GetAllActiveAsync(_currentUser.OwnerId, cancellationToken);
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
        var plan = await GetOwnedPlanAsync(targetPlanId, cancellationToken);

        var requestedIds = sourceAssignmentIds?.Distinct().ToHashSet() ?? [];
        if (requestedIds.Count == 0)
            return [];

        var allPlans = await _weeklyPlanRepository.GetAllAsync(_currentUser.OwnerId, cancellationToken);
        var sourceAssignments = allPlans
            .SelectMany(p => p.Assignments)
            .Where(a => requestedIds.Contains(a.Id))
            .ToList();

        var alreadyCopiedIds = new HashSet<Guid>();

        var copied = new List<TaskAssignmentDto>();
        foreach (var assignment in sourceAssignments)
        {
            if (!alreadyCopiedIds.Add(assignment.Id))
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
        var plan = await GetOwnedPlanAsync(weeklyPlanId, cancellationToken);

        var assignment = plan.Assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new NotFoundException(nameof(TaskAssignment), assignmentId);

        // Idempotency guard: already sent, don't create duplicate in Chrobi
        if (assignment.SentToExternalAt.HasValue)
            return SendTaskToExternalResult.Ok(assignment.ExternalCreatedTaskId ?? $"already-sent-{assignmentId}");

        Person person;
        try
        {
            person = await GetOwnedPersonAsync(assignment.PersonId, cancellationToken);
        }
        catch (NotFoundException)
        {
            return SendTaskToExternalResult.Fail("La persona asignada no existe.", 400);
        }
        if (!person.ChobiUserId.HasValue)
            return SendTaskToExternalResult.Fail($"'{person.Name}' no tiene configurado ChobiUserId.", 400);

        // ExternalTaskId format: "PROJ-{guid}"
        int? chobiProjectId = null;
        if (assignment.ExternalTaskId.StartsWith("PROJ-", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(assignment.ExternalTaskId[5..], out var projectGuid))
        {
            var project = await _projectRepository.GetByIdAsync(projectGuid, cancellationToken);
            if (project is null || !string.Equals(project.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase))
                return SendTaskToExternalResult.Fail("El proyecto referenciado en la asignación no existe.", 400);
            chobiProjectId = project.ChobiProjectId;
        }

        if (!chobiProjectId.HasValue)
            return SendTaskToExternalResult.Fail("La asignación no tiene un proyecto Chrobi vinculado.", 400);

        var creatorChobiUserId = await ResolveCreatorChobiUserIdAsync(cancellationToken);
        if (!creatorChobiUserId.HasValue)
            return SendTaskToExternalResult.Fail(
                "No tenés vinculado tu usuario de Chrobi. Agregate como persona con tu email y vinculala en Equipo.", 400);

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
            CreatorChobiUserId: creatorChobiUserId.Value);

        var result = await _externalService.SendAsync(request, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ExternalTaskId))
            return result;

        assignment.MarkAsSent(result.ExternalTaskId);
        await _weeklyPlanRepository.UpdateAsync(plan, cancellationToken);
        return result;
    }

    private async Task<WeeklyPlan> GetOwnedPlanAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklyPlan), id);

        if (!string.Equals(plan.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException(nameof(WeeklyPlan), id);

        return plan;
    }

    private async Task<Person> GetOwnedPersonAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), id);

        if (!string.Equals(person.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException(nameof(Person), id);

        return person;
    }

    /// <summary>
    /// Resuelve el ChobiUserId del PM actualmente logueado (no del asignado de la tarea).
    /// Si el PM no tiene todavía su propio registro de Person, lo crea; si no está
    /// vinculado a Chrobi, intenta vincularlo automáticamente por coincidencia de nombre
    /// contra la lista de usuarios activos de Chrobi.
    /// </summary>
    private async Task<int?> ResolveCreatorChobiUserIdAsync(CancellationToken cancellationToken)
    {
        var self = await _personRepository.GetByEmailAsync(
            _currentUser.OwnerId, _currentUser.OwnerId, cancellationToken);

        if (self is null)
        {
            self = Person.Create(_currentUser.OwnerId, _currentUser.UserName, _currentUser.OwnerId);
            await _personRepository.AddAsync(self, cancellationToken);
        }

        if (!self.ChobiUserId.HasValue)
        {
            var chobiUsers = await _chobiReadService.GetUsersAsync(cancellationToken);
            var match = chobiUsers.FirstOrDefault(u =>
                string.Equals(u.Description, self.Name, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                self.SetChobiUserId(match.Id);
                await _personRepository.UpdateAsync(self, cancellationToken);
            }
        }

        return self.ChobiUserId;
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
