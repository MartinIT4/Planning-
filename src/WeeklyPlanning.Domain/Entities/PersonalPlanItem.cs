namespace WeeklyPlanning.Domain.Entities;

public class PersonalPlanItem
{
    public Guid Id { get; private set; }
    public Guid PersonalWeeklyPlanId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Category { get; private set; } = "Task";
    public decimal? EstimatedHours { get; private set; }
    public int? PlannedDayOfWeek { get; private set; }
    public string? ExternalTaskId { get; private set; }
    public int? ChobiProjectId { get; private set; }
    public string? ExternalTaskUrl { get; private set; }
    public int SortOrder { get; private set; }
    public string Status { get; private set; } = "Planned";
    public bool IsDone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PersonalPlanItem() { }

    public static PersonalPlanItem Create(Guid planId, string title, string? description, string category, decimal? estimatedHours, int? plannedDayOfWeek, string? externalTaskId, int sortOrder, int? chobiProjectId)
    {
        return new PersonalPlanItem
        {
            Id = Guid.NewGuid(),
            PersonalWeeklyPlanId = planId,
            Title = title,
            Description = description,
            Category = category,
            EstimatedHours = estimatedHours,
            PlannedDayOfWeek = plannedDayOfWeek,
            ExternalTaskId = externalTaskId,
            ChobiProjectId = chobiProjectId,
            SortOrder = sortOrder,
            Status = "Planned",
            IsDone = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string? description, string category, decimal? estimatedHours, int? plannedDayOfWeek, int? chobiProjectId)
    {
        Title = title;
        Description = description;
        Category = category;
        EstimatedHours = estimatedHours;
        PlannedDayOfWeek = plannedDayOfWeek;
        ChobiProjectId = chobiProjectId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSentToExternal(string externalTaskId, string? externalTaskUrl)
    {
        ExternalTaskId = externalTaskId;
        ExternalTaskUrl = externalTaskUrl;
        Status = "SentToExternal";
        UpdatedAt = DateTime.UtcNow;
    }
}
