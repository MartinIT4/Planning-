namespace WeeklyPlanning.Application.DTOs;

public record PersonalPlanItemDto(
    Guid Id,
    Guid PersonalWeeklyPlanId,
    string Title,
    string? Description,
    string Category,
    decimal? EstimatedHours,
    int? PlannedDayOfWeek,
    string? ExternalTaskId,
    int? ChobiProjectId,
    string? ExternalTaskUrl,
    int SortOrder,
    string Status,
    bool IsDone,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PersonalWeeklyPlanDto(
    Guid Id,
    string OwnerId,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    string Status,
    string? Notes,
    IReadOnlyList<PersonalPlanItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreatePersonalPlanRequest(string OwnerId, DateOnly WeekStartDate, string? Notes);

public record CreatePersonalItemRequest(
    string Title,
    string? Description,
    string Category,
    decimal? EstimatedHours,
    int? PlannedDayOfWeek,
    string? ExternalTaskId,
    int? ChobiProjectId);

public record UpdatePersonalItemRequest(
    string Title,
    string? Description,
    string Category,
    decimal? EstimatedHours,
    int? PlannedDayOfWeek,
    int? ChobiProjectId);
