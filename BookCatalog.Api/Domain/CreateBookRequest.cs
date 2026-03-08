namespace BookCatalog.Api.Domain;

/// <summary>
/// Запрос на добавление книги в каталог.
/// </summary>
public sealed record CreateBookRequest(
    string? Title,
    string? Author,
    int Year,
    decimal Price
);
