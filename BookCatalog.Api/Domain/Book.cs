namespace BookCatalog.Api.Domain;

/// <summary>
/// Книга в каталоге.
/// </summary>
public sealed record Book(
    Guid Id,
    string Title,
    string Author,
    int Year,
    decimal Price
);
