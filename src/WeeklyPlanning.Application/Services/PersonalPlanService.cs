using Microsoft.Extensions.Configuration;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;

namespace WeeklyPlanning.Application.Services;

public class PersonalPlanService
{
    private const int DefaultCreatorUserId = 162;

    private readonly IPersonalPlanRepository _repo;
    private readonly IPersonRepository _personRepo;
    private readonly IExternalTaskCreationService _externalService;
    private readonly IConfiguration _config;

    public PersonalPlanService(
        IPersonalPlanRepository repo,
        IPersonRepository personRepo,
        IExternalTaskCreationService externalService,
        IConfiguration config)
    {
        _repo = repo;
        _personRepo = personRepo;
        _externalService = externalService;
        _config = config;
    }

    public async Task<PersonalWeeklyPlanDto?> GetByOwnerAndWeekAsync(string ownerId, DateOnly weekStartDate, CancellationToken ct = default)
    {
        var plan = await _repo.GetByOwnerAndWeekAsync(ownerId, weekStartDate, ct);
        return plan == null ? null : MapDto(plan);
    }

    public async Task<PersonalWeeklyPlanDto> CreateAsync(CreatePersonalPlanRequest req, CancellationToken ct = default)
    {
        var plan = PersonalWeeklyPlan.Create(req.OwnerId, req.WeekStartDate, req.Notes);
        await _repo.AddAsync(plan, ct);
        return MapDto(plan);
    }

    public async Task<PersonalWeeklyPlanDto> AddItemAsync(Guid planId, CreatePersonalItemRequest req, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(planId, ct) ?? throw new Exception("Plan not found");
        int sortOrder = plan.Items.Count > 0 ? plan.Items.Max(i => i.SortOrder) + 1 : 0;
        plan.AddItem(req.Title, req.Description, req.Category ?? "Task", req.EstimatedHours, req.PlannedDayOfWeek, req.ExternalTaskId, sortOrder, req.ChobiProjectId);
        await _repo.UpdateAsync(plan, ct);
        return MapDto(plan);
    }

    public async Task<PersonalWeeklyPlanDto> UpdateItemAsync(Guid planId, Guid itemId, UpdatePersonalItemRequest req, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(planId, ct) ?? throw new Exception("Plan not found");
        plan.UpdateItem(itemId, req.Title, req.Description, req.Category ?? "Task", req.EstimatedHours, req.PlannedDayOfWeek, req.ChobiProjectId);
        await _repo.UpdateAsync(plan, ct);
        return MapDto(plan);
    }

    public async Task<SendTaskToExternalResult> SendItemToExternalAsync(Guid planId, Guid itemId, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(planId, ct);
        if (plan is null)
            return SendTaskToExternalResult.Fail("El plan personal no existe.", 404);

        var item = plan.Items.FirstOrDefault(x => x.Id == itemId);
        if (item is null)
            return SendTaskToExternalResult.Fail("El ítem del plan no existe.", 404);

        var owner = await ResolveOwnerAsync(plan.OwnerId, ct);
        var creatorUserId = ResolveCreatorUserId();

        return await SendItemToExternalInternalAsync(plan, item, owner, creatorUserId, ct);
    }

    public async Task<PublishPersonalPlanToExternalResult> PublishWeekToExternalAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(planId, ct);
        if (plan is null)
            return new PublishPersonalPlanToExternalResult(false, "El plan personal no existe.", []);

        var owner = await ResolveOwnerAsync(plan.OwnerId, ct);
        var creatorUserId = ResolveCreatorUserId();

        var results = new List<PersonalPlanItemExternalPublishResult>();
        foreach (var item in plan.Items)
        {
            var result = await SendItemToExternalInternalAsync(plan, item, owner, creatorUserId, ct);
            results.Add(new PersonalPlanItemExternalPublishResult(
                item.Id,
                item.Title,
                result.Success,
                result.ExternalTaskId,
                result.ExternalTaskUrl,
                result.ErrorMessage,
                result.HttpStatusCode));
        }

        return new PublishPersonalPlanToExternalResult(results.All(x => x.Success), null, results);
    }

    public async Task DeleteItemAsync(Guid planId, Guid itemId, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(planId, ct) ?? throw new Exception("Plan not found");
        plan.RemoveItem(itemId);
        await _repo.UpdateAsync(plan, ct);
    }

    private async Task<SendTaskToExternalResult> SendItemToExternalInternalAsync(
        PersonalWeeklyPlan plan,
        PersonalPlanItem item,
        Person? owner,
        int creatorUserId,
        CancellationToken ct)
    {
        if (!item.ChobiProjectId.HasValue)
            return SendTaskToExternalResult.Fail("El ítem no tiene configurado ChobiProjectId.", 400);

        if (owner is null)
            return SendTaskToExternalResult.Fail("No se pudo resolver la persona owner del plan personal.", 400);

        if (!owner.ChobiUserId.HasValue)
            return SendTaskToExternalResult.Fail("La persona asignada no tiene configurado ChobiUserId.", 400);

        var request = new SendTaskToExternalRequest(
            PersonalPlanItemId: item.Id,
            Title: item.Title,
            Description: item.Description,
            EstimatedHours: item.EstimatedHours,
            AssigneeChobiUserId: owner.ChobiUserId.Value,
            ChobiProjectId: item.ChobiProjectId,
            WeekStartDate: plan.WeekStartDate,
            WeekEndDate: plan.WeekEndDate,
            CreatorChobiUserId: creatorUserId);

        var result = await _externalService.SendAsync(request, ct);
        if (!result.Success || string.IsNullOrWhiteSpace(result.ExternalTaskId))
            return result;

        plan.MarkItemAsSentToExternal(item.Id, result.ExternalTaskId, result.ExternalTaskUrl);
        await _repo.UpdateAsync(plan, ct);
        return result;
    }

    private async Task<Person?> ResolveOwnerAsync(string ownerId, CancellationToken ct)
    {
        if (Guid.TryParse(ownerId, out var ownerGuid))
            return await _personRepo.GetByIdAsync(ownerGuid, ct);

        return await _personRepo.GetByEmailAsync(ownerId, ct);
    }

    private int ResolveCreatorUserId() =>
        int.TryParse(_config["ExternalApi:CreatorUserId"], out var creatorUserId)
            ? creatorUserId
            : DefaultCreatorUserId;

    private static PersonalWeeklyPlanDto MapDto(PersonalWeeklyPlan p) => new(
        p.Id, p.OwnerId, p.WeekStartDate, p.WeekEndDate, p.Status, p.Notes,
        p.Items.Select(i => new PersonalPlanItemDto(
            i.Id, i.PersonalWeeklyPlanId, i.Title, i.Description, i.Category,
            i.EstimatedHours, i.PlannedDayOfWeek, i.ExternalTaskId, i.ChobiProjectId, i.ExternalTaskUrl, i.SortOrder,
            i.Status, i.IsDone, i.CreatedAt, i.UpdatedAt)).ToList(),
        p.CreatedAt, p.UpdatedAt);
}
