namespace BookCatalog.Api.Services;

/// <summary>
/// Вспомогательный класс для работы с идентификатором запроса.
/// Идентификатор хранится в Items контекста, чтобы быть доступным
/// из любого слоя конвейера без явной передачи.
/// </summary>
public static class RequestId
{
    private const string Key = "X-Request-Id";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(Key, out var existing) && existing is string id)
            return id;

        var newId = context.Request.Headers[Key].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N")[..12];

        context.Items[Key] = newId;
        return newId;
    }
}
