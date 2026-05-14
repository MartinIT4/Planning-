using WeeklyPlanning.Domain.Enums;

namespace WeeklyPlanning.Application.DTOs;

public record WeeklyPlanDto(
    Guid Id,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    string Status,
    string? Notes,
    IEnumerable<TaskAssignmentDto> Assignments,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TaskAssignmentDto(
    Guid Id,
    Guid WeeklyPlanId,
    Guid PersonId,
    string PersonName,
    string ExternalTaskId,
    string TaskTitle,
    decimal PlannedHours,
    string? Notes,
    DateTime? SentToExternalAt,
    string? ExternalCreatedTaskId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PersonDto(
    Guid Id,
    string Name,
    string Email,
    decimal WeeklyCapacityHours,
    int? ChobiUserId,
    bool IsActive
);

public record CapacitySummaryDto(
    Guid WeeklyPlanId,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    IEnumerable<PersonCapacityDto> PersonCapacities
);

public record PersonCapacityDto(
    Guid PersonId,
    string PersonName,
    decimal WeeklyCapacityHours,
    decimal PlannedHours,
    decimal RemainingHours,
    bool IsOverCapacity,
    string? WarningMessage
);
