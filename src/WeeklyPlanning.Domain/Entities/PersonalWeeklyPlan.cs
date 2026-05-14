namespace WeeklyPlanning.Domain.Entities;

public class PersonalWeeklyPlan
{
    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = default!;
    public DateOnly WeekStartDate { get; private set; }
    public DateOnly WeekEndDate { get; private set; }
    public string Status { get; private set; } = "Draft";
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private List<PersonalPlanItem> _items = new();
    public IReadOnlyList<PersonalPlanItem> Items => _items.AsReadOnly();

    private PersonalWeeklyPlan() { }

    public static PersonalWeeklyPlan Create(string ownerId, DateOnly weekStartDate, string? notes = null)
    {
        return new PersonalWeeklyPlan
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            WeekStartDate = weekStartDate,
            WeekEndDate = weekStartDate.AddDays(4),
            Status = "Draft",
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public PersonalPlanItem AddItem(string title, string? description, string category, decimal? estimatedHours, int? plannedDayOfWeek, string? externalTaskId, int sortOrder, int? chobiProjectId)
    {
        var item = PersonalPlanItem.Create(Id, title, description, category, estimatedHours, plannedDayOfWeek, externalTaskId, sortOrder, chobiProjectId);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    public void UpdateItem(Guid itemId, string title, string? description, string category, decimal? estimatedHours, int? plannedDayOfWeek, int? chobiProjectId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new Exception($"Item {itemId} not found");
        item.Update(title, description, category, estimatedHours, plannedDayOfWeek, chobiProjectId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkItemAsSentToExternal(Guid itemId, string externalTaskId, string? externalTaskUrl)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new Exception($"Item {itemId} not found");
        item.MarkAsSentToExternal(externalTaskId, externalTaskUrl);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new Exception($"Item {itemId} not found");
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }
}
