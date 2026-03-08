using System.Collections.Concurrent;
using BookCatalog.Api.Domain;

namespace BookCatalog.Api.Services;

/// <summary>
/// Потокобезопасное хранилище книг в памяти процесса.
/// ConcurrentDictionary гарантирует атомарность чтения и записи
/// одиночных элементов без явных блокировок.
/// </summary>
public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();

    public IReadOnlyCollection<Book> GetAll(string? titleFilter = null, string? authorFilter = null)
    {
        var query = _books.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(titleFilter))
            query = query.Where(b => b.Title.Contains(titleFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(authorFilter))
            query = query.Where(b => b.Author.Contains(authorFilter, StringComparison.OrdinalIgnoreCase));

        return query
            .OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Book? GetById(Guid id)
        => _books.TryGetValue(id, out var book) ? book : null;

    public Book Create(string title, string author, int year, decimal price)
    {
        var id = Guid.NewGuid();
        var book = new Book(id, title, author, year, price);
        _books[id] = book;
        return book;
    }
}
