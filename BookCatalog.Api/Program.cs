using System.Text.Json;
using System.Text.Json.Serialization;
using BookCatalog.Api.Domain;
using BookCatalog.Api.Errors;
using BookCatalog.Api.Middlewares;
using BookCatalog.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TimingAndLogMiddleware>();

// GET /api/books?title=...&author=...
app.MapGet("/api/books", async (HttpContext ctx, IBookRepository repo) =>
{
    var title  = ctx.Request.Query.TryGetValue("title",  out var t) ? t.ToString() : null;
    var author = ctx.Request.Query.TryGetValue("author", out var a) ? a.ToString() : null;
    var books = repo.GetAll(
        string.IsNullOrEmpty(title)  ? null : title,
        string.IsNullOrEmpty(author) ? null : author);
    await WriteJson(ctx, 200, books);
});

// GET /api/books/{id}
app.MapGet("/api/books/{id:guid}", async (Guid id, HttpContext ctx, IBookRepository repo) =>
{
    var book = repo.GetById(id);
    if (book is null)
        throw new NotFoundException($"Книга с идентификатором '{id}' не найдена");
    await WriteJson(ctx, 200, book);
});

// POST /api/books
app.MapPost("/api/books", async (HttpContext ctx, IBookRepository repo) =>
{
    CreateBookRequest? request;
    try
    {
        request = await JsonSerializer.DeserializeAsync<CreateBookRequest>(
            ctx.Request.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        throw new ValidationException("Тело запроса содержит некорректный JSON");
    }

    if (request is null)
        throw new ValidationException("Тело запроса не должно быть пустым");

    if (string.IsNullOrWhiteSpace(request.Title))
        throw new ValidationException("Поле title не должно быть пустым");

    if (string.IsNullOrWhiteSpace(request.Author))
        throw new ValidationException("Поле author не должно быть пустым");

    if (request.Year < 1450 || request.Year > DateTime.UtcNow.Year)
        throw new ValidationException($"Поле year должно быть в диапазоне 1450–{DateTime.UtcNow.Year}");

    if (request.Price < 0)
        throw new ValidationException("Поле price не может быть отрицательным");

    if (request.Title.Trim().Length > 200)
        throw new ValidationException("Поле title не должно превышать 200 символов");

    var created = repo.Create(
        request.Title.Trim(),
        request.Author.Trim(),
        request.Year,
        request.Price);

    var location = $"/api/books/{created.Id}";
    ctx.Response.Headers.Location = location;
    await WriteJson(ctx, 201, created);
});

app.Run();

// Единый метод записи JSON-ответа — не использует PipeWriter,
// поэтому совместим с WebApplicationFactory в .NET 9.
static async Task WriteJson<T>(HttpContext ctx, int status, T value)
{
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json; charset=utf-8";
    await ctx.Response.WriteAsync(JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    }));
}

public partial class Program { }
