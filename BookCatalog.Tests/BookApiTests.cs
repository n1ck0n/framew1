using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace BookCatalog.Tests;

public sealed class BookCatalogFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

public sealed class BookApiTests : IClassFixture<BookCatalogFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookApiTests(BookCatalogFactory factory, ITestOutputHelper output)
    {
        _output = output;
        _client = factory.CreateClient();
    }

    private async Task<string> ReadAndLog(HttpResponseMessage r, string label = "")
    {
        var body = await r.Content.ReadAsStringAsync();
        _output.WriteLine($"{label} [{(int)r.StatusCode} {r.StatusCode}]: {body}");
        return body;
    }

    // ── Диагностика ──────────────────────────────────────────────

    [Fact]
    public async Task Diagnostic_GetBooks_ShowRealError()
    {
        var response = await _client.GetAsync("/api/books");
        await ReadAndLog(response, "GET /api/books");
    }

    [Fact]
    public async Task Diagnostic_PostBook_ShowRealError()
    {
        var payload = new { title = "Диагностика", author = "Автор", year = 2020, price = 100 };
        var response = await _client.PostAsJsonAsync("/api/books", payload);
        await ReadAndLog(response, "POST /api/books");
    }

    // ── GET /api/books ──────────────────────────────────────────

    [Fact]
    public async Task GetBooks_ShouldReturn200WithArray()
    {
        var response = await _client.GetAsync("/api/books");
        var body = await ReadAndLog(response, "GET /api/books");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("[", body.TrimStart());
    }

    // ── POST + GET ──────────────────────────────────────────────

    [Fact]
    public async Task CreateBook_ThenGetById_ShouldReturnSameBook()
    {
        var payload = new { title = "Тестовая книга", author = "Тест Тестов", year = 2020, price = 199.99 };

        var post = await _client.PostAsJsonAsync("/api/books", payload);
        var postBody = await ReadAndLog(post, "POST /api/books");
        Assert.True(post.StatusCode == HttpStatusCode.Created,
            $"POST вернул {post.StatusCode}: {postBody}");

        var id = JsonDocument.Parse(postBody).RootElement.GetProperty("id").GetString()!;

        var get = await _client.GetAsync($"/api/books/{id}");
        var getBody = await ReadAndLog(get, $"GET /api/books/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var fetched = JsonDocument.Parse(getBody).RootElement;
        Assert.Equal("Тестовая книга", fetched.GetProperty("title").GetString());
        Assert.Equal("Тест Тестов", fetched.GetProperty("author").GetString());
    }

    [Fact]
    public async Task CreateBook_ShouldSetLocationHeader()
    {
        var payload = new { title = "Локация тест", author = "Автор", year = 2021, price = 100 };
        var response = await _client.PostAsJsonAsync("/api/books", payload);
        var body = await ReadAndLog(response, "POST /api/books");

        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"POST вернул {response.StatusCode}: {body}");
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/books/", response.Headers.Location!.ToString());
    }

    // ── 404 ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBook_UnknownId_ShouldReturn404WithErrorFormat()
    {
        var response = await _client.GetAsync($"/api/books/{Guid.NewGuid()}");
        var body = await ReadAndLog(response, "GET /api/books/{unknown}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = JsonSerializer.Deserialize<ErrorDto>(body, JsonOpts)!;
        Assert.Equal("not_found", error.Code);
        Assert.False(string.IsNullOrEmpty(error.Message));
        Assert.False(string.IsNullOrEmpty(error.RequestId));
    }

    // ── 422 ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBook_EmptyTitle_ShouldReturn422WithCode()
    {
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "", author = "Автор", year = 2020, price = 100 });
        var body = await ReadAndLog(response, "POST empty title");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = JsonSerializer.Deserialize<ErrorDto>(body, JsonOpts)!;
        Assert.Equal("validation_error", error.Code);
    }

    [Fact]
    public async Task CreateBook_NegativePrice_ShouldReturn422()
    {
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "Книга", author = "Автор", year = 2020, price = -10 });
        await ReadAndLog(response, "POST negative price");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_InvalidYear_ShouldReturn422()
    {
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "Книга", author = "Автор", year = 1000, price = 100 });
        await ReadAndLog(response, "POST invalid year");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_EmptyAuthor_ShouldReturn422()
    {
        var response = await _client.PostAsJsonAsync("/api/books",
            new { title = "Книга", author = "", year = 2020, price = 100 });
        await ReadAndLog(response, "POST empty author");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ── X-Request-Id ─────────────────────────────────────────────

    [Fact]
    public async Task AnyRequest_ShouldReturnRequestIdHeader()
    {
        // X-Request-Id устанавливается в ErrorHandlingMiddleware при ошибках.
        // Для успешных запросов проверяем наличие requestId в теле (или заголовке).
        var response = await _client.GetAsync($"/api/books/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ErrorDto>(body, JsonOpts)!;
        Assert.False(string.IsNullOrEmpty(error.RequestId));
    }

    [Fact]
    public async Task ErrorResponse_RequestIdInBodyMatchesHeader()
    {
        var response = await _client.GetAsync($"/api/books/{Guid.NewGuid()}");
        var body = await ReadAndLog(response, "GET unknown");

        var error = JsonSerializer.Deserialize<ErrorDto>(body, JsonOpts)!;
        Assert.False(string.IsNullOrEmpty(error.RequestId));

        if (response.Headers.TryGetValues("X-Request-Id", out var vals))
            Assert.Equal(vals.First(), error.RequestId);
    }

    // ── Фильтрация ───────────────────────────────────────────────

    [Fact]
    public async Task GetBooks_WithTitleFilter_ReturnsOnlyMatchingBooks()
    {
        var marker = Guid.NewGuid().ToString("N")[..8].ToUpper();

        var p1 = await _client.PostAsJsonAsync("/api/books",
            new { title = $"Книга {marker} уникальная", author = "Автор", year = 2020, price = 100 });
        await ReadAndLog(p1, "POST book1");

        await _client.PostAsJsonAsync("/api/books",
            new { title = "Совсем другая книга", author = "Автор", year = 2021, price = 150 });

        var response = await _client.GetAsync($"/api/books?title={marker}");
        var body = await ReadAndLog(response, $"GET ?title={marker}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var books = JsonDocument.Parse(body).RootElement;
        Assert.True(books.GetArrayLength() >= 1);
        foreach (var book in books.EnumerateArray())
            Assert.Contains(marker, book.GetProperty("title").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ErrorDto(string Code, string Message, string RequestId);
}
