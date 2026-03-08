using BookCatalog.Api.Services;

namespace BookCatalog.Api.Middlewares;

/// <summary>
/// Первый шаг конвейера — назначает идентификатор запроса.
/// Должен стоять раньше всех остальных обработчиков, чтобы
/// идентификатор был доступен при логировании ошибок.
/// </summary>
public sealed class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        _ = RequestId.GetOrCreate(context);
        return _next(context);
    }
}
