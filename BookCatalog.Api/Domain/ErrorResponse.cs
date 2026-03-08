namespace BookCatalog.Api.Domain;

/// <summary>
/// Единый формат ошибки, который возвращается клиенту.
/// Содержит машинночитаемый код, человекочитаемое сообщение
/// и идентификатор запроса для поиска в журнале.
/// </summary>
public sealed record ErrorResponse(string Code, string Message, string RequestId);
