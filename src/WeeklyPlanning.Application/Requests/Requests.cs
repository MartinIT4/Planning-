namespace WeeklyPlanning.Application.Requests;

public record CreateWeeklyPlanRequest(
    DateOnly WeekStartDate,
    string? Notes
);

public record UpdateWeeklyPlanRequest(
    string? Notes
);

public record UpdateWeeklyPlanStatusRequest(
    string Action  // "confirm" | "close" | "revert-to-draft"
);

public record AddTaskAssignmentRequest(
    Guid PersonId,
    string ExternalTaskId,
    string TaskTitle,
    decimal PlannedHours,
    string? Notes
);

public record UpdateTaskAssignmentRequest(
    string TaskTitle,
    decimal PlannedHours,
    string? Notes
);

public record CopyAssignmentsRequest(
    IEnumerable<Guid> SourceAssignmentIds
);

public record CreatePersonRequest(
    string Name,
    decimal WeeklyCapacityHours = 40,
    string? Email = null
);

public record UpdatePersonRequest(
    string Name,
    decimal WeeklyCapacityHours,
    string? Email = null
);
