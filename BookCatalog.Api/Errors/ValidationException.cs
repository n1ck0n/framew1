namespace BookCatalog.Api.Errors;

/// <summary>
/// Входные данные нарушают правила предметной области → HTTP 422.
/// </summary>
public sealed class ValidationException : DomainException
{
    public ValidationException(string message)
        : base("validation_error", message, 422) { }
}
