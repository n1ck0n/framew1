using System.Text.Json;
using System.Text.Json.Serialization;
using BookCatalog.Api.Domain;
using BookCatalog.Api.Errors;
using BookCatalog.Api.Services;

namespace BookCatalog.Api.Middlewares;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = RequestId.GetOrCreate(context);

        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Ошибка предметной области. requestId={RequestId}", requestId);
            await WriteError(context, ex.StatusCode, ex.Code, ex.Message, requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка. requestId={RequestId}", requestId);

            // В тестовом/dev окружении показываем реальное исключение в ответе
            var detail = _env.IsEnvironment("Testing") || _env.IsDevelopment()
                ? ex.ToString()
                : null;

            await WriteError(context, 500, "internal_error",
                detail ?? "Внутренняя ошибка сервера", requestId);
        }
    }

    private static async Task WriteError(
        HttpContext context, int statusCode, string code, string message, string requestId)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers["X-Request-Id"] = requestId;

        var payload = new ErrorResponse(code, message, requestId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
