namespace WeeklyPlanning.Application.DTOs;

public record ChobiUserDto(int Id, string Description);

public record ChobiProjectDto(int Id, string Name, string? ClientName, bool IsBillable);

public record ChobiSyncResultDto(
    int PersonsLinked,
    int PersonsCreated,
    int ProjectsLinked,
    int ProjectsCreated
);
