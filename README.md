# BookCatalog — мини веб-служба на ASP.NET Core 9

Каталог книг с REST API, конвейером обработки запросов и тестами.

---

## Быстрый старт

**Требования:** .NET 9 SDK

```bash
# Клонировать / распаковать проект, затем:
cd BookCatalog
dotnet run --project BookCatalog.Api
```

Служба запускается на `http://localhost:5000`.

---

## Запуск тестов

```bash
dotnet test BookCatalog.Tests
```

Ожидаемый вывод: все тесты зелёные, покрыты юнит- и интеграционный сценарии.

---

## Ручной прогон сценариев

### 1. Получить список книг (изначально пуст)

```bash
curl -s http://localhost:5000/api/books | jq
```

Ожидаемый ответ: `[]`

---

### 2. Создать книгу

```bash
curl -s -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{"title":"Мастер и Маргарита","author":"Михаил Булгаков","year":1967,"price":320}' | jq
```

Ожидаемый ответ (HTTP 201):
```json
{
  "id": "...",
  "title": "Мастер и Маргарита",
  "author": "Михаил Булгаков",
  "year": 1967,
  "price": 320
}
```

Запомните значение `id` из ответа.

---

### 3. Получить книгу по идентификатору

```bash
# Замените {ID} на значение из предыдущего шага
curl -s http://localhost:5000/api/books/{ID} | jq
```

---

### 4. Запрос по несуществующему идентификатору → 404

```bash
curl -s http://localhost:5000/api/books/00000000-0000-0000-0000-000000000000 | jq
```

Ожидаемый ответ (HTTP 404):
```json
{
  "code": "not_found",
  "message": "Книга с идентификатором '...' не найдена",
  "requestId": "abc123"
}
```

---

### 5. Создать книгу с пустым названием → 422

```bash
curl -s -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{"title":"","author":"Автор","year":2020,"price":100}' | jq
```

Ожидаемый ответ (HTTP 422):
```json
{
  "code": "validation_error",
  "message": "Поле title не должно быть пустым",
  "requestId": "..."
}
```

---

### 6. Создать книгу с отрицательной ценой → 422

```bash
curl -s -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{"title":"Книга","author":"Автор","year":2020,"price":-10}' | jq
```

---

### 7. Фильтрация по названию

```bash
curl -s "http://localhost:5000/api/books?title=Мастер" | jq
```

---

### 8. Посмотреть заголовок X-Request-Id

```bash
curl -si http://localhost:5000/api/books | grep -i x-request-id
```

---

## Архитектура конвейера

```
Запрос → RequestIdMiddleware → ErrorHandlingMiddleware → TimingAndLogMiddleware → Endpoint
```

| Шаг                    | Ответственность                                      |
|------------------------|------------------------------------------------------|
| RequestIdMiddleware    | Назначить / принять X-Request-Id, записать в Items   |
| ErrorHandlingMiddleware| Перехватить исключения, сформировать ErrorResponse   |
| TimingAndLogMiddleware | Измерить время, записать структурированное событие   |

---

## Правила предметной области

| Поле   | Правило                                           |
|--------|---------------------------------------------------|
| title  | Непустая строка, не длиннее 200 символов          |
| author | Непустая строка                                   |
| year   | Целое число в диапазоне 1450 — текущий год        |
| price  | Неотрицательное число                             |
