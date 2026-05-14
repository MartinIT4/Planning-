namespace WeeklyPlanning.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string resource, object id)
        : base($"{resource} con id '{id}' no encontrado.") { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message) : base(message)
        => Errors = new[] { message };

    public ValidationException(IEnumerable<string> errors)
        : base("Uno o más errores de validación ocurrieron.")
        => Errors = errors;
}
