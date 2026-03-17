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

## Ручной прогон сценариев (PowerShell)

Все команды используют `Invoke-RestMethod` (встроенный cmdlet PowerShell) для корректной работы с JSON.

### 1. Получить список книг (изначально пуст)

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/books" -Method Get | ConvertTo-Json
```

Ожидаемый ответ: `[]`

---

### 2. Создать книгу

```powershell
$body = '{"title":"The Master and Margarita","author":"Mikhail Bulgakov","year":1967,"price":320}'; Invoke-RestMethod -Uri "http://localhost:5000/api/books" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json
```

Ожидаемый ответ (HTTP 201):
```json
{
  "id": "...",
  "title": "The Master and Margarita",
  "author": "Mikhail Bulgakov",
  "year": 1967,
  "price": 320
}
```

Запомните значение `id` из ответа.

---

### 3. Получить книгу по идентификатору

```powershell
# Замените {ID} на значение из предыдущего шага
Invoke-RestMethod -Uri "http://localhost:5000/api/books/{ID}" -Method Get | ConvertTo-Json
```

---

### 4. Запрос по несуществующему идентификатору → 404

```powershell
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/books/00000000-0000-0000-0000-000000000000" -Method Get -ErrorAction Stop | ConvertTo-Json
} catch {
    $_.Exception.Response | Format-List
}
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

```powershell
$body = '{"title":"","author":"Author","year":2020,"price":100}'; Invoke-RestMethod -Uri "http://localhost:5000/api/books" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json
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

```powershell
$body = '{"title":"Book","author":"Author","year":2020,"price":-10}'; Invoke-RestMethod -Uri "http://localhost:5000/api/books" -Method Post -Body $body -ContentType "application/json" | ConvertTo-Json
```

---

### 7. Фильтрация по названию

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/books?title=Master" -Method Get | ConvertTo-Json
```

---

### 8. Посмотреть заголовок X-Request-Id

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/books" -Method Get -UseBasicParsing | Select-Object -ExpandProperty Headers | Select-Object "X-Request-Id"
```

Или более кратко:
```powershell
(Invoke-WebRequest -Uri "http://localhost:5000/api/books" -UseBasicParsing).Headers["X-Request-Id"]
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
```
