using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;

namespace WeeklyPlanning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/chrobi")]
[Produces("application/json")]
public class ChobiController : ControllerBase
{
    private readonly IChobiReadService _chobiReadService;
    private readonly IPersonRepository _personRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUser;

    public ChobiController(
        IChobiReadService chobiReadService,
        IPersonRepository personRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUser)
    {
        _chobiReadService = chobiReadService;
        _personRepository = personRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<ChobiUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken) =>
        Ok(await _chobiReadService.GetUsersAsync(cancellationToken));

    [HttpGet("projects")]
    [ProducesResponseType(typeof(IReadOnlyList<ChobiProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken) =>
        Ok(await _chobiReadService.GetProjectsAsync(cancellationToken));

    [HttpPost("sync")]
    [ProducesResponseType(typeof(ChobiSyncResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var chobiUsers = await _chobiReadService.GetUsersAsync(cancellationToken);
        var localPersons = (await _personRepository.GetAllActiveAsync(_currentUser.OwnerId, cancellationToken)).ToList();

        var personsLinked = 0;
        var personsCreated = 0;

        foreach (var user in chobiUsers)
        {
            var person = localPersons.FirstOrDefault(p =>
                string.Equals(p.Name, user.Description, StringComparison.OrdinalIgnoreCase));

            if (person is null)
            {
                person = Person.Create(_currentUser.OwnerId, user.Description, null, 40);
                person.SetChobiUserId(user.Id);
                await _personRepository.AddAsync(person, cancellationToken);
                localPersons.Add(person);
                personsCreated++;
                continue;
            }

            if (person.ChobiUserId.HasValue)
                continue;

            person.SetChobiUserId(user.Id);
            await _personRepository.UpdateAsync(person, cancellationToken);
            personsLinked++;
        }

        var chobiProjects = await _chobiReadService.GetProjectsAsync(cancellationToken);
        var localProjects = (await _projectRepository.GetAllActiveAsync(_currentUser.OwnerId, cancellationToken)).ToList();

        var projectsLinked = 0;
        var projectsCreated = 0;

        foreach (var chobiProject in chobiProjects)
        {
            var project = localProjects.FirstOrDefault(p => p.ChobiProjectId == chobiProject.Id)
                ?? localProjects.FirstOrDefault(p =>
                    string.Equals(p.Name, chobiProject.Name, StringComparison.OrdinalIgnoreCase));

            if (project is null)
            {
                project = Project.Create(_currentUser.OwnerId, chobiProject.Name, chobiProject.ClientName, chobiProject.IsBillable);
                project.SetChobiProjectId(chobiProject.Id);
                await _projectRepository.AddAsync(project, cancellationToken);
                localProjects.Add(project);
                projectsCreated++;
                continue;
            }

            if (project.ChobiProjectId.HasValue)
                continue;

            project.SetChobiProjectId(chobiProject.Id);
            await _projectRepository.UpdateAsync(project, cancellationToken);
            projectsLinked++;
        }

        return Ok(new ChobiSyncResultDto(personsLinked, personsCreated, projectsLinked, projectsCreated));
    }
}
