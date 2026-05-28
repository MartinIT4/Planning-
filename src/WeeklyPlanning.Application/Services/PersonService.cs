using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Exceptions;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Application.Requests;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Exceptions;
using WeeklyPlanning.Domain.Interfaces;

namespace WeeklyPlanning.Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly ICurrentUserService _currentUser;

    public PersonService(IPersonRepository personRepository, ICurrentUserService currentUser)
    {
        _personRepository = personRepository;
        _currentUser = currentUser;
    }

    public async Task<PersonDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await GetOwnedPersonAsync(id, cancellationToken);
        return MapToDto(person);
    }

    public async Task<IEnumerable<PersonDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var persons = await _personRepository.GetAllActiveAsync(_currentUser.OwnerId, cancellationToken);
        return persons.Select(MapToDto);
    }

    public async Task<PersonDto> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existing = await _personRepository.GetByEmailAsync(request.Email, _currentUser.OwnerId, cancellationToken);
            if (existing is not null)
                throw new ConflictException($"Ya existe una persona con el email '{request.Email}'.");
        }

        Person person;
        try
        {
            person = Person.Create(_currentUser.OwnerId, request.Name, request.Email, request.WeeklyCapacityHours);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _personRepository.AddAsync(person, cancellationToken);
        return MapToDto(person);
    }

    public async Task<PersonDto> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var person = await GetOwnedPersonAsync(id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailOwner = await _personRepository.GetByEmailAsync(request.Email, _currentUser.OwnerId, cancellationToken);
            if (emailOwner is not null && emailOwner.Id != id)
                throw new ConflictException($"El email '{request.Email}' ya está en uso por otra persona.");
        }

        try
        {
            person.Update(request.Name, request.Email, request.WeeklyCapacityHours);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _personRepository.UpdateAsync(person, cancellationToken);
        return MapToDto(person);
    }

    public async Task<PersonDto> SetChobiUserIdAsync(Guid id, int chobiUserId, CancellationToken cancellationToken = default)
    {
        if (chobiUserId <= 0)
            throw new ValidationException("El ChobiUserId debe ser mayor a cero.");

        var person = await GetOwnedPersonAsync(id, cancellationToken);

        person.SetChobiUserId(chobiUserId);
        await _personRepository.UpdateAsync(person, cancellationToken);
        return MapToDto(person);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await GetOwnedPersonAsync(id, cancellationToken);

        person.Deactivate();
        await _personRepository.UpdateAsync(person, cancellationToken);
    }

    private async Task<Person> GetOwnedPersonAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), id);

        if (!string.Equals(person.OwnerId, _currentUser.OwnerId, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException(nameof(Person), id);

        return person;
    }

    private static PersonDto MapToDto(Person p) =>
        new(p.Id, p.Name, p.Email, p.WeeklyCapacityHours, p.ChobiUserId, p.IsActive);
}
