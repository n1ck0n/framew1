namespace BookCatalog.Api.Errors;

/// <summary>
/// Запрошенный ресурс не найден → HTTP 404.
/// </summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base("not_found", message, 404) { }
}
