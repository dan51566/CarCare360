# CarCare360 API

REST API серверной части платформы CarCare360. Предоставляет полный набор эндпоинтов для мобильного приложения клиентов и служит общим бэкендом данных для десктопного WPF-приложения.

> **Связанные проекты:** [CarCare360.Desktop](../README.md) · [carcare_mobile](../carcare_mobile/README.md) · [Корневой README](../README.md)

---

## Содержание

1. [Общая информация](#1-общая-информация)
2. [Функциональные возможности](#2-функциональные-возможности)
3. [База данных](#3-база-данных)
4. [Аутентификация и безопасность](#4-аутентификация-и-безопасность)
5. [Структура проекта](#5-структура-проекта)
6. [Инструкция по запуску](#6-инструкция-по-запуску)
7. [Описание эндпоинтов](#7-описание-эндпоинтов)
8. [Формат JSON и ошибок](#8-формат-json-и-ошибок)
9. [Производительность и безопасность](#9-производительность-и-безопасность)
10. [Дополнительные сведения](#10-дополнительные-сведения)

---

## 1. Общая информация

| Параметр | Значение |
|----------|----------|
| **Тип** | ASP.NET Core 10 Web API |
| **Язык** | C# 13 |
| **ORM** | Entity Framework Core 10 |
| **База данных** | SQL Server 2019 Express (`.\SQLEXPRESS`) |
| **Аутентификация** | JWT Bearer (access + refresh токены) |
| **Порт по умолчанию** | `5009` |
| **Документация API** | Swagger UI (`/swagger`, только Development) |

### Стек технологий

| Пакет | Версия | Назначение |
|-------|--------|------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.0 | JWT-аутентификация |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.0 | ORM для SQL Server |
| `BCrypt.Net-Next` | 4.0.3 | Хеширование паролей |
| `Swashbuckle.AspNetCore` | 6.9.0 | Swagger/OpenAPI документация |

### Архитектура

API является **тонким слоем транспорта** над существующей базой данных CarCare360, которую также использует десктопное WPF-приложение. Бизнес-логика сосредоточена в слое сервисов; контроллеры только маршрутизируют запросы. Хранимые процедуры SQL Server используются для критичных операций (создание заказа, смена статуса) в соответствии с соглашениями десктопного приложения.

---

## 2. Функциональные возможности

### Контроллеры

| Контроллер | Маршрут | Назначение |
|------------|---------|------------|
| `AuthController` | `/api/auth` | Вход, регистрация, обновление токена, выход |
| `ClientsController` | `/api/clients` | Управление профилем клиентов |
| `CarsController` | `/api/cars` | CRUD автомобилей клиента |
| `OrdersController` | `/api/orders` | Заказы-наряды: создание, просмотр, смена статуса |
| `ServicesController` | `/api/services` | Справочник услуг (публичный GET) |
| `PartsController` | `/api/parts` | Склад запчастей (только персонал) |
| `MechanicsController` | `/api/mechanics` | Список механиков и их расписание |
| `ReportsController` | `/api/reports` | Финансовый отчёт, нагрузка механиков |
| `AuditController` | `/api/audit` | Журнал аудита с пагинацией |
| `CarModelsController` | `/api/car-models` | Публичный справочник марок и моделей |

### Роли пользователей

| Роль | Доступ |
|------|--------|
| **Администратор** | Полный доступ ко всем ресурсам |
| **Механик** | Заказы, назначенные ему; справочники (только чтение) |
| **Клиент** | Только свои автомобили, заказы и профиль |

### Статусы заказов

Машина состояний из 7 статусов (как в десктопном приложении):
`Новый` → `Назначен` → `В работе` → `Ожидает запчасти` → `Готов` → `Выдан`
Также возможен переход в `Отменён` из любого не-финального статуса.

---

## 3. База данных

### Подключение

API использует **ту же базу данных** `CarCare360`, что и десктопное приложение. Строка подключения в `appsettings.json`:

```json
"ConnectionStrings": {
  "CarCareDb": "Server=.\\SQLEXPRESS;Database=CarCare360;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Схема данных

15 таблиц (14 оригинальных + 1 для refresh-токенов):

| Таблица | Назначение |
|---------|------------|
| `Users` | Все пользователи системы (клиенты, механики, администраторы) |
| `Roles` | Роли: Администратор, Механик, Клиент |
| `Cars` | Автомобили клиентов |
| `CarBrands` | Справочник марок автомобилей |
| `CarModels` | Справочник моделей автомобилей |
| `Services` | Каталог услуг автосервиса |
| `Parts` | Склад запчастей |
| `Mechanics` | Профили механиков |
| `Specializations` | Специализации механиков |
| `ServiceBays` | Рабочие посты автосервиса |
| `Orders` | Заказы-наряды |
| `OrderServices` | Услуги в составе заказа (с привязкой механика и поста) |
| `OrderParts` | Запчасти в составе заказа |
| `AuditLogs` | Журнал аудита (заполняется триггерами) |
| `RefreshTokens` | JWT refresh-токены с отзывом |

### Мягкое удаление

- `Users`: поля `IsDeleted` (bool) + `IsActive` (bool)
- `Orders`: поле `IsDeleted` (bool)
- `Cars`: **жёсткое** удаление с защитой 409 Conflict при наличии связанных заказов

### Хранимые процедуры

Используются через `Data/StoredProcedureRunner.cs`:

| Процедура | Назначение |
|-----------|------------|
| `CreateOrder` | Создать заказ с автоматической установкой статуса «Новый» |
| `AddServiceToOrder` | Добавить услугу к существующему заказу |
| `UpdateOrderStatus` | Изменить статус заказа |
| `AddPartToOrder` | Добавить запчасть к заказу |
| `UpdateOrderPart` | Обновить количество/цену запчасти |

> ⚠️ После вызова хранимых процедур необходимо перечитывать сущности с `AsNoTracking()`, так как EF Core не отслеживает изменения, сделанные в обход его трекера.

### Аудит-триггеры

Таблицы `Users`, `Cars`, `Orders` имеют SQL-триггеры (I/U/D), которые автоматически записывают изменения в `AuditLogs`. Это требует явного объявления триггеров в `DbContext`:

```csharp
entity.ToTable(tb => tb.HasTrigger("trg_Audit_Orders"));
```

### Дополнительный SQL-скрипт

Таблица `RefreshTokens` создаётся отдельным скриптом (не входит в исходную схему десктопа):

```bash
sqlcmd -S .\SQLEXPRESS -d CarCare360 -i Database/03_Create_RefreshTokens.sql
```

---

## 4. Аутентификация и безопасность

### JWT-токены

| Параметр | Значение |
|----------|----------|
| **Схема** | Bearer |
| **Access-токен** | Время жизни 15 минут |
| **Refresh-токен** | Время жизни 7 дней, ротация при обновлении |
| **Clock skew** | 30 секунд |
| **Алгоритм** | HMAC-SHA256 |

Пример заголовка запроса:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Хеширование паролей

Используется **BCrypt** (work-factor 12) с совместимостью с десктопным приложением: хеш хранится в поле `BINARY(64)` (BCrypt-строка 60 символов + нулевой паддинг до 64 байт). Вспомогательный класс `Helpers/PasswordHelper.cs`.

### Rate Limiting

Эндпоинты аутентификации защищены от брутфорса:
- Алгоритм: `SlidingWindow`
- Лимит: **3 запроса в минуту** с одного IP
- Применяется к: `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/refresh`
- Ответ при превышении: `429 Too Many Requests`

### Обработка ошибок

Единый формат всех ошибок через `Helpers/ExceptionHandlingMiddleware.cs`:

```json
{
  "error": "Описание ошибки для пользователя",
  "detail": "Стектрейс (только в Development, null в Production)"
}
```

| Код | Сценарий |
|-----|----------|
| `400` | Невалидные данные запроса |
| `401` | Отсутствует или невалидный токен |
| `403` | Недостаточно прав (попытка доступа к чужим данным) |
| `404` | Ресурс не найден |
| `409` | Конфликт (дубликат логина, удаление машины с заказами) |
| `429` | Превышен лимит запросов к эндпоинтам аутентификации |

---

## 5. Структура проекта

```
CarCare360.Api/
├── Controllers/          # HTTP-маршруты, атрибуты авторизации
├── Services/             # Интерфейсы + реализации бизнес-логики
│   ├── IAuthService.cs / AuthService.cs
│   ├── ICarService.cs   / CarService.cs
│   ├── IOrderService.cs / OrderService.cs
│   ├── ICatalogService.cs / CatalogService.cs
│   ├── IClientService.cs / ClientService.cs
│   ├── IMechanicService.cs / MechanicService.cs
│   ├── IReportService.cs / ReportService.cs
│   ├── IAuditService.cs / AuditService.cs
│   ├── ITokenService.cs / TokenService.cs
│   └── LoginAttemptTracker.cs   (rate-limit helper)
├── Models/
│   ├── Entities/         # EF Core сущности (совпадают с десктопом)
│   └── Dtos/             # DTO запросов/ответов
│       ├── AuthDtos.cs   (LoginRequest, RegisterRequest, AuthResponse, UserDto)
│       ├── CarDtos.cs    (CarDto, CarCreateRequest, CarUpdateRequest)
│       ├── OrderDtos.cs  (OrderDto, OrderCreateRequest, OrderStatusUpdateRequest)
│       ├── CatalogDtos.cs (ServiceDto, PartDto, CarModelDto)
│       ├── ClientDtos.cs (ClientUpdateRequest)
│       ├── MechanicDtos.cs (MechanicDto, MechanicScheduleItemDto)
│       ├── ReportDtos.cs (FinancialReportDto, MechanicsLoadReportDto)
│       └── AuditDtos.cs  (AuditLogDto, PagedResult<T>)
├── Helpers/
│   ├── CurrentUser.cs    # Извлечение UserId/Role из JWT claims
│   ├── Roles.cs          # Константы ролей
│   ├── OrderStatuses.cs  # Константы статусов заказов
│   ├── PasswordHelper.cs # BCrypt + BINARY(64) совместимость
│   ├── JwtSettings.cs    # Конфигурация JWT
│   ├── ApiException.cs   # Пользовательское исключение
│   └── ExceptionHandlingMiddleware.cs
├── Data/
│   ├── CarCareDbContext.cs      # EF Core DbContext
│   ├── StoredProcedureRunner.cs # ADO.NET вызов хранимых процедур
│   └── ApiSeeder.cs             # Засев тест-данных (отключён)
├── Database/
│   └── 03_Create_RefreshTokens.sql
├── Program.cs            # Точка входа, конфигурация middleware
└── appsettings.json      # Строка подключения, JWT-параметры
```

---

## 6. Инструкция по запуску

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server Express 2019+ с уже созданной базой `CarCare360` (создаётся при первом запуске [CarCare360.Desktop](../README.md))
- Доступ к `.\SQLEXPRESS` через Windows Authentication (Trusted_Connection)

### Шаг 1 — Применить скрипт таблицы RefreshTokens

Если база данных уже создана десктопным приложением, добавьте таблицу для refresh-токенов:

```bash
sqlcmd -S .\SQLEXPRESS -d CarCare360 -i Database/03_Create_RefreshTokens.sql
```

Или откройте файл в SQL Server Management Studio и выполните.

### Шаг 2 — Настроить `appsettings.json`

```json
{
  "ConnectionStrings": {
    "CarCareDb": "Server=.\\SQLEXPRESS;Database=CarCare360;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "ЗАМЕНИТЕ-НА-СЛУЧАЙНУЮ-СТРОКУ-ДЛИНОЙ-64+-СИМВОЛА",
    "Issuer": "CarCare360.Api",
    "Audience": "CarCare360.Clients",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

> ⚠️ **Обязательно** замените `Jwt.Key` перед развёртыванием в production!

### Шаг 3 — Восстановить зависимости и запустить

```bash
cd CarCare360.Api
dotnet restore
dotnet run --urls http://localhost:5009
```

Или для явного указания окружения:

```bash
$env:ASPNETCORE_ENVIRONMENT = "Development"   # PowerShell
dotnet run --no-launch-profile --urls http://localhost:5009
```

### Шаг 4 — Проверить работоспособность

- **Swagger UI:** [http://localhost:5009/swagger](http://localhost:5009/swagger) (только в Development)
- **Тестовый запрос:**

```powershell
$body = '{"Login":"admin","Password":"admin123"}' | `
    [System.Text.Encoding]::UTF8.GetBytes
Invoke-RestMethod http://localhost:5009/api/auth/login `
    -Method Post -Body $body -ContentType 'application/json; charset=utf-8'
```

Ожидаемый ответ:
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "uPKb...",
  "accessTokenExpiresAt": "2026-05-29T03:03:54Z",
  "user": {
    "userID": 1,
    "login": "admin",
    "fullName": "Администратор Системы",
    "role": "Администратор",
    "isActive": true
  }
}
```

### Тестовые учётные записи

| Логин | Пароль | Роль |
|-------|--------|------|
| `admin` | `admin123` | Администратор |
| `mechanic` | `mechanic123` | Механик |
| `vadim` | `vadim123` | Клиент (Мильденберг В.В.) |

> Учётные записи создаются десктопным приложением при первом запуске (`DatabaseSeeder`).

---

## 7. Описание эндпоинтов

### POST /api/auth/login
Вход в систему. Rate-limit: 3 запроса/мин с IP.

**Запрос:**
```json
{ "Login": "vadim", "Password": "vadim123" }
```
**Ответ `200`:** `AuthResponse` (см. Шаг 4 выше)
**Ответ `401`:** `{ "error": "Неверный логин или пароль." }`

---

### POST /api/auth/register
Регистрация нового клиента.

**Запрос:**
```json
{
  "Login": "newclient",
  "Password": "pass123",
  "FullName": "Иванов Иван Иванович",
  "Email": "ivan@example.com",
  "Phone": "+7 900 000-00-00"
}
```
**Ответ `200`:** `AuthResponse` (автоматический вход)
**Ответ `409`:** Логин уже занят

---

### POST /api/auth/refresh
Обновление access-токена по refresh-токену (ротация).

**Запрос:** `{ "RefreshToken": "uPKb..." }`
**Ответ `200`:** Новый `AuthResponse`

---

### POST /api/auth/logout
Отзыв refresh-токена. Требует `Authorization: Bearer`.

**Запрос:** `{ "RefreshToken": "uPKb..." }`
**Ответ `204`:** No Content

---

### GET /api/cars
Список автомобилей. Клиент видит только свои; администратор — все.

**Ответ `200`:**
```json
[
  {
    "carID": 1,
    "clientID": 6,
    "clientName": "Мильденберг Вадим Владимирович",
    "modelID": 42,
    "brandName": "Mercedes-Benz",
    "modelName": "S-Class",
    "year": 2021,
    "vin": "WDD2231761A012345",
    "licensePlate": "А777АА777",
    "color": "Чёрный",
    "mileage": 12500
  }
]
```

---

### POST /api/cars
Добавить автомобиль. Клиент добавляет себе; администратор может указать `ClientID`.

**Запрос:**
```json
{
  "ModelID": 42,
  "Year": 2021,
  "VIN": "WDD2231761A012345",
  "LicensePlate": "А777АА777",
  "Color": "Чёрный",
  "Mileage": 12500
}
```

---

### GET /api/orders
Список заказов. Клиент — только свои; механик — назначенные ему; администратор — все.

**Ответ `200`:** массив `OrderDto`:
```json
[
  {
    "orderID": 1,
    "carID": 1,
    "carInfo": "Mercedes-Benz S-Class (А777АА777)",
    "clientID": 6,
    "clientName": "Мильденберг Вадим Владимирович",
    "createdAt": "2026-05-29T06:44:53",
    "scheduledDate": "2026-06-01T00:00:00",
    "scheduledTime": "10:30",
    "status": "Выдан",
    "notes": null,
    "partsTotal": 3500.0,
    "services": [...],
    "parts": [...]
  }
]
```

---

### POST /api/orders
Создать заказ. Доступно клиенту и администратору.

**Запрос:**
```json
{
  "CarID": 1,
  "ScheduledDate": "2026-06-10T00:00:00",
  "ScheduledTime": "14:00",
  "Notes": "Стук при торможении",
  "ServiceIds": [1, 3]
}
```

---

### PUT /api/orders/{id}/status
Изменить статус заказа. **Только администратор и механик.**

**Запрос:** `{ "Status": "В работе" }`

---

### DELETE /api/orders/{id}
Мягкое удаление заказа (`IsDeleted = true`). Клиент может удалить только свой заказ — это механизм **отмены записи** в мобильном приложении.

---

### GET /api/services
Список услуг автосервиса. **Публичный** (не требует авторизации).

**Ответ `200`:**
```json
[
  {
    "serviceID": 1,
    "name": "Замена масла",
    "description": "Замена моторного масла и масляного фильтра",
    "normHour": 0.5,
    "basePrice": 500.00
  }
]
```

---

### GET /api/car-models
Справочник марок и моделей. **Публичный** (используется в форме добавления авто).

**Ответ `200`:**
```json
[
  { "modelID": 64, "brandID": 12, "brandName": "Alfa Romeo", "name": "147" },
  { "modelID": 42, "brandID": 8,  "brandName": "Mercedes-Benz", "name": "S-Class" }
]
```

---

### GET /api/reports/financial?from=2026-01-01&to=2026-12-31
Финансовый отчёт за период. **Только администратор.**

**Ответ `200`:**
```json
{
  "servicesRevenue": 150000.0,
  "partsRevenue": 45000.0,
  "totalRevenue": 195000.0
}
```

---

### GET /api/audit?tableName=Orders&from=2026-01-01&page=1&pageSize=50
Журнал аудита с пагинацией. **Только администратор.**

**Ответ `200`:**
```json
{
  "total": 124,
  "page": 1,
  "pageSize": 50,
  "items": [
    {
      "logID": 1,
      "tableName": "Orders",
      "operation": "I",
      "changedAt": "2026-05-29T06:44:53Z",
      "changedBy": "API"
    }
  ]
}
```

---

## 8. Формат JSON и ошибок

### Соглашения по именованию

API возвращает **camelCase с сохранением акронимов**:

| Поле | Формат в JSON |
|------|---------------|
| UserID | `userID` |
| CarID | `carID` |
| OrderID | `orderID` |
| VIN | `vin` |
| AccessTokenExpiresAt | `accessTokenExpiresAt` |

Поля запросов принимаются без учёта регистра (ASP.NET Core автоматически десериализует `UserID`, `userId`, `userid`).

### Формат дат и времени

- Даты: ISO 8601, UTC (`2026-06-01T00:00:00Z`)
- Время записи: строка `"HH:mm"` (например `"14:30"`)

---

## 9. Производительность и безопасность

### Текущие настройки (Development)

| Параметр | Значение |
|----------|----------|
| CORS | AllowAll (`*`) — только для разработки |
| HTTPS | Редирект включён |
| Swagger | Доступен только в `ASPNETCORE_ENVIRONMENT=Development` |
| Cleartext HTTP | Разрешён в манифесте Android-эмулятора (`usesCleartextTraffic`) |

### Рекомендации для Production

- Заменить `Jwt.Key` на случайную строку длиной 64+ символа
- Ограничить CORS конкретными доменами/приложениями
- Использовать HTTPS (TLS-сертификат)
- Убрать `TrustServerCertificate=True` из строки подключения
- Выключить Swagger или защитить его авторизацией
- Настроить `appsettings.Production.json` с боевыми параметрами

### Индексы базы данных

На критичных полях созданы индексы (из скрипта создания БД):
- `Users.Login` (unique)
- `Cars.LicensePlate` (unique)
- `Cars.VIN`
- `RefreshTokens.Token` (unique)
- `RefreshTokens.UserID`
- `Orders.ClientID`
- `Orders.CarID`

---

## 10. Дополнительные сведения

### Связанные проекты

| Проект | Описание |
|--------|----------|
| [CarCare360.Desktop](../README.md) | WPF-приложение для персонала автосервиса (администратор, механик) |
| [carcare_mobile](../carcare_mobile/README.md) | Flutter-приложение для клиентов (Android/iOS) |

### Статус разработки

- ✅ Реализовано: все CRUD-операции, JWT, rate limiting, аудит, отчёты
- 🔲 Запланировано: эндпоинт регистрации FCM-токена устройства (`POST /api/notifications/register-device`), загрузка аватаров клиентов, смена пароля через API

### Образовательный контекст

Проект создан в учебных целях. Для production-развёртывания потребуются дополнительные меры безопасности: сертификаты, отдельный Identity-сервер, мониторинг, резервное копирование БД.
