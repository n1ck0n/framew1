using BookCatalog.Api.Domain;

namespace BookCatalog.Api.Services;

/// <summary>
/// Контракт хранилища книг.
/// Абстракция позволяет подменить реализацию в тестах или при переходе на БД.
/// </summary>
public interface IBookRepository
{
    IReadOnlyCollection<Book> GetAll(string? titleFilter = null, string? authorFilter = null);
    Book? GetById(Guid id);
    Book Create(string title, string author, int year, decimal price);
}
