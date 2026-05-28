namespace WeeklyPlanning.Application.Interfaces;

public interface ICurrentUserService
{
    string OwnerId { get; }
    string UserName { get; }
}
