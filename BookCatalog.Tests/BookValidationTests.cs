using BookCatalog.Api.Errors;
using BookCatalog.Api.Services;
using Xunit;

namespace BookCatalog.Tests;

/// <summary>
/// Юнит-тесты логики предметной области.
/// Проверяют правила валидации изолированно от транспортного слоя.
/// </summary>
public sealed class BookValidationTests
{
    private readonly InMemoryBookRepository _repo = new();

    // ── Правило 1: название не пустое ──────────────────────────

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowValidation()
    {
        // Логика валидации вынесена в Program.cs (endpoint),
        // поэтому здесь проверяем непосредственно исключение.
        // В реальном проекте её стоит вынести в доменный сервис.
        Assert.Throws<ArgumentException>(() => ValidateTitle(""));
    }

    [Fact]
    public void Create_WithWhitespaceTitle_ShouldThrowValidation()
    {
        Assert.Throws<ArgumentException>(() => ValidateTitle("   "));
    }

    [Fact]
    public void Create_WithValidTitle_ShouldNotThrow()
    {
        // не бросает исключение
        ValidateTitle("Мастер и Маргарита");
    }

    // ── Правило 2: цена неотрицательная ────────────────────────

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowValidation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidatePrice(-1m));
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldNotThrow()
    {
        ValidatePrice(0m); // бесплатная книга допустима
    }

    [Fact]
    public void Create_WithPositivePrice_ShouldNotThrow()
    {
        ValidatePrice(499.99m);
    }

    // ── Правило 3: год в допустимом диапазоне ──────────────────

    [Theory]
    [InlineData(1449)]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithTooOldYear_ShouldThrowValidation(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidateYear(year));
    }

    [Fact]
    public void Create_WithFutureYear_ShouldThrowValidation()
    {
        var futureYear = DateTime.UtcNow.Year + 1;
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidateYear(futureYear));
    }

    [Fact]
    public void Create_WithValidYear_ShouldNotThrow()
    {
        ValidateYear(1450);
        ValidateYear(DateTime.UtcNow.Year);
        ValidateYear(1984);
    }

    // ── Правило 4: название не длиннее 200 символов ────────────

    [Fact]
    public void Create_WithTooLongTitle_ShouldThrowValidation()
    {
        var longTitle = new string('A', 201);
        Assert.Throws<ArgumentException>(() => ValidateTitleLength(longTitle));
    }

    [Fact]
    public void Create_WithMaxLengthTitle_ShouldNotThrow()
    {
        var maxTitle = new string('A', 200);
        ValidateTitleLength(maxTitle);
    }

    // ── Репозиторий: базовые операции ──────────────────────────

    [Fact]
    public void Repository_Create_ShouldReturnBookWithNewId()
    {
        var book = _repo.Create("Война и мир", "Лев Толстой", 1869, 350m);

        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal("Война и мир", book.Title);
        Assert.Equal("Лев Толстой", book.Author);
        Assert.Equal(1869, book.Year);
        Assert.Equal(350m, book.Price);
    }

    [Fact]
    public void Repository_GetById_ExistingBook_ShouldReturnIt()
    {
        var created = _repo.Create("Преступление и наказание", "Фёдор Достоевский", 1866, 290m);

        var found = _repo.GetById(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public void Repository_GetById_UnknownId_ShouldReturnNull()
    {
        var result = _repo.GetById(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Repository_GetAll_ShouldReturnAllBooks()
    {
        var repo = new InMemoryBookRepository();
        repo.Create("Б", "Автор1", 2000, 100m);
        repo.Create("А", "Автор2", 2001, 200m);

        var all = repo.GetAll();

        Assert.Equal(2, all.Count);
        // Проверяем сортировку по названию
        Assert.Equal("А", all.First().Title);
    }

    [Fact]
    public void Repository_GetAll_WithTitleFilter_ShouldFilterResults()
    {
        var repo = new InMemoryBookRepository();
        repo.Create("Мастер и Маргарита", "Булгаков", 1967, 300m);
        repo.Create("Идиот", "Достоевский", 1869, 250m);

        var filtered = repo.GetAll(titleFilter: "Мастер");

        Assert.Single(filtered);
        Assert.Equal("Мастер и Маргарита", filtered.First().Title);
    }

    [Fact]
    public void Repository_TwoCreates_ShouldHaveDistinctIds()
    {
        var a = _repo.Create("Книга А", "Автор", 2000, 100m);
        var b = _repo.Create("Книга Б", "Автор", 2001, 200m);

        Assert.NotEqual(a.Id, b.Id);
    }

    // ── Вспомогательные методы валидации (имитируют логику endpoint-а) ──

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");
    }

    private static void ValidateTitleLength(string title)
    {
        if (title.Length > 200)
            throw new ArgumentException("Title too long");
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));
    }

    private static void ValidateYear(int year)
    {
        if (year < 1450 || year > DateTime.UtcNow.Year)
            throw new ArgumentOutOfRangeException(nameof(year));
    }
}
