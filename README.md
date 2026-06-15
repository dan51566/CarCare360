# CarCare 360

Десктопное WPF-приложение для сотрудников автосервиса — администраторов и механиков.  
Реализовано на **.NET 10 + C#** с архитектурой **MVVM**, ORM **Entity Framework Core 10** и СУБД **Microsoft SQL Server 2019 Express**.

> **Статус:** десктопная версия полностью реализована.  
> Также реализованы REST API (ASP.NET Core, каталог `CarCare360.Api`) и мобильное приложение для клиентов (Flutter, каталог `carcare_mobile`).

---

## Содержание

1. [Общая информация](#1-общая-информация)
2. [Функциональные возможности](#2-функциональные-возможности)
3. [База данных](#3-база-данных)
4. [Архитектура приложения](#4-архитектура-приложения)
5. [Производительность и временные характеристики](#5-производительность-и-временные-характеристики)
6. [Инструкция по развёртыванию и запуску](#6-инструкция-по-развёртыванию-и-запуску)
7. [Безопасность](#7-безопасность)
8. [Диагностика и тестирование](#8-диагностика-и-тестирование)
9. [Визуальный стиль и анимации](#9-визуальный-стиль-и-анимации)
10. [Дополнительные сведения](#10-дополнительные-сведения)

---

## 1. Общая информация

| Параметр | Значение |
|---|---|
| **Название** | CarCare 360 |
| **Тип** | Десктопное приложение (Windows) |
| **Фреймворк** | WPF / .NET 10.0-windows |
| **Язык** | C# (latest) |
| **Архитектура** | MVVM (Model-View-ViewModel) |
| **ORM** | Entity Framework Core 10.0.0 |
| **СУБД** | Microsoft SQL Server 2019 Express |
| **Экземпляр БД** | `.\SQLEXPRESS`, база `CarCare360` |
| **Хеширование паролей** | BCrypt.Net-Next 4.0.3 |
| **Формат решения** | `.slnx` (современный формат Visual Studio) |

### Технологический стек

```
CarCare360.Desktop.csproj
├── Microsoft.EntityFrameworkCore.SqlServer  10.0.0
├── Microsoft.EntityFrameworkCore.Design     10.0.0
├── Microsoft.EntityFrameworkCore.Tools      10.0.0
├── BCrypt.Net-Next                          4.0.3
└── System.Configuration.ConfigurationManager 10.0.0
```

---

## 2. Функциональные возможности

Приложение организовано вокруг **8 модулей**. Доступность разделов зависит от роли пользователя (см. [Безопасность → RBAC](#7-безопасность)).

### 2.1 Клиенты

- Список клиентов с поиском по имени, телефону, e-mail
- Создание, редактирование профиля клиента
- Регистрация новых клиентов (логин + bcrypt-пароль)
- Просмотр автомобилей и истории заказов клиента
- Мягкое удаление (`IsDeleted = 1`) — данные сохраняются в БД
- Аватар пользователя (`UserAvatarStorage`)

### 2.2 Автомобили

- Привязка автомобиля к клиенту
- Поля: марка / модель (из справочника), год выпуска, VIN (17 символов), госномер, цвет, пробег
- Индексы по VIN и госномеру для быстрого поиска
- CRUD-диалоги (`CarEditDialog`)

### 2.3 Заказы (Заказ-наряды)

- Создание заказ-наряда с выбором автомобиля, даты и времени
- Статусная машина из **7 состояний**: `Новый → Назначен → В работе → Ожидает запчасти → Готов → Выдан → Отменён`
- Детализация заказа: список услуг (с назначением механика и бокса) и список запчастей
- Добавление услуг через `OrderServices`, запчастей через `OrderParts`
- Контроль остатка запчастей на складе при списании (хранимая процедура с транзакцией)
- Диалоги создания (`OrderCreateDialog`) и просмотра (`OrderDetailDialog`)

### 2.4 Склад

- Каталог запчастей с артикулом (`PartNumber`), ценой и количеством
- Визуальный индикатор низкого остатка (`QuantityToLowStockConverter`)
- Списание запчастей при добавлении в заказ (атомарная транзакция через `AddPartToOrder`)
- CRUD через `PartEditDialog`

### 2.5 Механики

- Список механиков с фото профиля, специализацией и уровнем квалификации
- Дата найма, профессиональный стаж
- Назначение механика на конкретную услугу внутри заказа
- `MechanicEditDialog` для создания и редактирования

### 2.6 Справочники

- **Марки автомобилей** — название, страна производства
- **Модели автомобилей** — привязаны к марке, годы выпуска
- **Виды услуг** — наименование, описание, нормо-час, базовая цена
- **Специализации механиков**
- **Боксы / рабочие посты** — наименование, статус активности
- Каждый справочник — отдельный ViewModel (`BaysRefViewModel`, `BrandsRefViewModel`, `ServicesRefViewModel`, `SpecializationsRefViewModel`)

### 2.7 Аудит

- Автоматическое журналирование всех операций INSERT / UPDATE / DELETE в таблицах `Users`, `Cars`, `Orders` — через SQL-триггеры
- Таблица `AuditLog`: имя таблицы, тип операции (I/U/D), первичный ключ записи, пользователь БД, время, старые и новые значения в JSON
- `AuditView` — фильтрация и поиск по журналу в интерфейсе
- Индексы по `TableName` и `ChangedAt` для быстрой фильтрации

### 2.8 Отчёты

- Раздел `ReportsView` / `ReportsViewModel` реализован
- Финансовый дашборд (KPI, выручка), графики (LiveCharts2) и сводка нагрузки механиков реализованы
- Экспорт в Excel — заглушка, запланирован в следующей итерации

---

## 3. База данных

### Общие сведения

| Параметр | Значение |
|---|---|
| СУБД | Microsoft SQL Server 2019 Express |
| Экземпляр | `.\SQLEXPRESS` |
| База данных | `CarCare360` |
| Таблицы | 14 |
| Хранимые процедуры | 5 |
| Триггеры аудита | 3 |
| Индексы | 8 (некластеризованных) |

### Схема таблиц

| Таблица | Назначение |
|---|---|
| `Roles` | Роли пользователей (Администратор, Механик, Клиент) |
| `Users` | Все пользователи системы — и клиенты, и сотрудники |
| `CarBrands` | Справочник марок автомобилей |
| `CarModels` | Справочник моделей (FK → CarBrands) |
| `Cars` | Автомобили клиентов (FK → Users, CarModels) |
| `Specializations` | Специализации механиков |
| `Mechanics` | Профиль механика (FK → Users, Specializations) |
| `ServiceBays` | Боксы / рабочие посты |
| `Services` | Виды услуг с нормо-часами и ценой |
| `Parts` | Запчасти со складским остатком |
| `Orders` | Заказ-наряды (FK → Cars, Users) |
| `OrderServices` | Услуги внутри заказа (FK → Orders, Services, Mechanics, ServiceBays) |
| `OrderParts` | Запчасти внутри заказа (FK → Orders, Parts) |
| `AuditLog` | Журнал всех изменений (заполняется триггерами) |

### Хранимые процедуры

| Процедура | Назначение |
|---|---|
| `RegisterClient` | Регистрация нового клиента (определяет RoleID автоматически) |
| `CreateOrder` | Создание заказ-наряда |
| `AddServiceToOrder` | Добавление услуги в заказ с назначением механика и бокса |
| `UpdateOrderStatus` | Смена статуса заказа с валидацией допустимых значений |
| `AddPartToOrder` | Атомарное списание запчасти: INSERT в OrderParts + уменьшение QuantityInStock |

---

### Скрипт 1: Создание структуры БД

> Выполнить первым. Создаёт все таблицы, индексы, роли и хранимые процедуры.

```sql
-- ================================================================
-- Файл: 01_Create_Database.sql
-- Назначение: Создание структуры БД CarCare 360 (таблицы, индексы,
--             связи, начальные данные, хранимые процедуры)
-- СУБД: Microsoft SQL Server 2019+
-- ================================================================
USE CarCare360;
GO

-- ================================================================
-- 1. СПРАВОЧНИКИ
-- ================================================================

-- Роли пользователей (Role-Based Access Control)
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200)
);

-- Пользователи системы (клиенты, механики, администраторы)
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash BINARY(64) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    RoleID INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    IsDeleted BIT DEFAULT 0,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);
CREATE NONCLUSTERED INDEX IX_Users_Phone ON Users(Phone);
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);

-- Марки автомобилей
CREATE TABLE CarBrands (
    BrandID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Country NVARCHAR(50)
);

-- Модели автомобилей
CREATE TABLE CarModels (
    ModelID INT IDENTITY(1,1) PRIMARY KEY,
    BrandID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    YearFrom INT,
    YearTo INT,
    CONSTRAINT FK_CarModels_Brands FOREIGN KEY (BrandID) REFERENCES CarBrands(BrandID),
    CONSTRAINT UQ_CarModel UNIQUE (BrandID, Name)
);

-- Автомобили клиентов
CREATE TABLE Cars (
    CarID INT IDENTITY(1,1) PRIMARY KEY,
    ClientID INT NOT NULL,
    ModelID INT NOT NULL,
    Year INT,
    VIN NVARCHAR(17),
    LicensePlate NVARCHAR(9) NOT NULL,
    Color NVARCHAR(30),
    Mileage INT,
    CONSTRAINT FK_Cars_Users FOREIGN KEY (ClientID) REFERENCES Users(UserID),
    CONSTRAINT FK_Cars_Models FOREIGN KEY (ModelID) REFERENCES CarModels(ModelID)
);
CREATE NONCLUSTERED INDEX IX_Cars_LicensePlate ON Cars(LicensePlate);
CREATE NONCLUSTERED INDEX IX_Cars_VIN ON Cars(VIN);

-- Специализации механиков
CREATE TABLE Specializations (
    SpecID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL UNIQUE
);

-- Механики (расширение таблицы Users)
CREATE TABLE Mechanics (
    MechanicID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    SpecializationID INT,
    HireDate DATE,
    QualificationLevel NVARCHAR(30),
    CONSTRAINT FK_Mechanics_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Mechanics_Spec FOREIGN KEY (SpecializationID) REFERENCES Specializations(SpecID)
);

-- Боксы / рабочие посты
CREATE TABLE ServiceBays (
    BayID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(20) NOT NULL UNIQUE,
    IsActive BIT DEFAULT 1
);

-- Виды услуг (работ)
CREATE TABLE Services (
    ServiceID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200),
    NormHour DECIMAL(4,2) NOT NULL,
    BasePrice DECIMAL(10,2)
);

-- Запчасти (склад)
CREATE TABLE Parts (
    PartID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    PartNumber NVARCHAR(50) UNIQUE,
    QuantityInStock INT DEFAULT 0,
    Price DECIMAL(10,2)
);

-- ================================================================
-- 2. ЗАКАЗ-НАРЯДЫ (основная бизнес-сущность)
-- ================================================================

CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CarID INT NOT NULL,
    ClientID INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ScheduledDate DATE,
    ScheduledTime TIME,
    Status NVARCHAR(20) DEFAULT 'Новый'
        CHECK (Status IN ('Новый','Назначен','В работе','Ожидает запчасти','Готов','Выдан','Отменён')),
    Mileage INT,
    Notes NVARCHAR(500),
    IsDeleted BIT DEFAULT 0,
    CONSTRAINT FK_Orders_Cars FOREIGN KEY (CarID) REFERENCES Cars(CarID),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (ClientID) REFERENCES Users(UserID)
);
CREATE NONCLUSTERED INDEX IX_Orders_Status ON Orders(Status);

-- Услуги, включённые в заказ (детализация)
CREATE TABLE OrderServices (
    OrderServiceID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    ServiceID INT NOT NULL,
    MechanicID INT,
    BayID INT,
    StartTime DATETIME2,
    EndTime DATETIME2,
    Status NVARCHAR(20) DEFAULT 'Назначена'
        CHECK (Status IN ('Назначена','В работе','Выполнена','Отменена')),
    CONSTRAINT FK_OS_Order FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    CONSTRAINT FK_OS_Service FOREIGN KEY (ServiceID) REFERENCES Services(ServiceID),
    CONSTRAINT FK_OS_Mechanic FOREIGN KEY (MechanicID) REFERENCES Mechanics(MechanicID),
    CONSTRAINT FK_OS_Bay FOREIGN KEY (BayID) REFERENCES ServiceBays(BayID)
);

-- Запчасти, использованные в заказе
CREATE TABLE OrderParts (
    OrderPartID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    PartID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PricePerUnit DECIMAL(10,2),
    CONSTRAINT FK_OP_Order FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    CONSTRAINT FK_OP_Part FOREIGN KEY (PartID) REFERENCES Parts(PartID)
);

-- ================================================================
-- 3. ЖУРНАЛ АУДИТА БЕЗОПАСНОСТИ
-- ================================================================

CREATE TABLE AuditLog (
    LogID BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(50) NOT NULL,
    Operation CHAR(1) NOT NULL CHECK (Operation IN ('I','U','D')),
    PrimaryKeyValue NVARCHAR(100),
    ChangedBy NVARCHAR(100),
    ChangedAt DATETIME2 DEFAULT GETDATE(),
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IPAddress NVARCHAR(45)
);
CREATE NONCLUSTERED INDEX IX_Audit_TableName ON AuditLog(TableName);
CREATE NONCLUSTERED INDEX IX_Audit_ChangedAt ON AuditLog(ChangedAt);

-- ================================================================
-- 4. НАЧАЛЬНЫЕ ДАННЫЕ (роли)
-- ================================================================

INSERT INTO Roles (Name, Description) VALUES 
('Администратор', 'Полный доступ к системе'),
('Механик', 'Выполнение и просмотр заказов'),
('Клиент', 'Мобильное приложение, запись на сервис');

-- ================================================================
-- 5. ХРАНИМЫЕ ПРОЦЕДУРЫ (безопасное выполнение операций)
-- ================================================================

-- Регистрация нового клиента
GO
CREATE PROCEDURE RegisterClient
    @Login NVARCHAR(50),
    @PasswordHash BINARY(64),
    @FullName NVARCHAR(100),
    @Email NVARCHAR(100),
    @Phone NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RoleID INT = (SELECT RoleID FROM Roles WHERE Name = 'Клиент');
    INSERT INTO Users (Login, PasswordHash, FullName, Email, Phone, RoleID)
    VALUES (@Login, @PasswordHash, @FullName, @Email, @Phone, @RoleID);
    SELECT SCOPE_IDENTITY() AS NewUserID;
END;
GO

-- Создание заказ-наряда
CREATE PROCEDURE CreateOrder
    @CarID INT,
    @ClientID INT,
    @ScheduledDate DATE,
    @ScheduledTime TIME,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Orders (CarID, ClientID, ScheduledDate, ScheduledTime, Notes)
    VALUES (@CarID, @ClientID, @ScheduledDate, @ScheduledTime, @Notes);
    SELECT SCOPE_IDENTITY() AS NewOrderID;
END;
GO

-- Добавление услуги в заказ
CREATE PROCEDURE AddServiceToOrder
    @OrderID INT,
    @ServiceID INT,
    @MechanicID INT = NULL,
    @BayID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrderServices (OrderID, ServiceID, MechanicID, BayID)
    VALUES (@OrderID, @ServiceID, @MechanicID, @BayID);
END;
GO

-- Обновление статуса заказа
CREATE PROCEDURE UpdateOrderStatus
    @OrderID INT,
    @NewStatus NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    IF @NewStatus NOT IN ('Новый','Назначен','В работе','Ожидает запчасти','Готов','Выдан','Отменён')
        RAISERROR('Недопустимый статус', 16, 1);
    UPDATE Orders SET Status = @NewStatus WHERE OrderID = @OrderID;
END;
GO

-- Списание запчастей (добавление в заказ и уменьшение остатка)
CREATE PROCEDURE AddPartToOrder
    @OrderID INT,
    @PartID INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF (SELECT QuantityInStock FROM Parts WHERE PartID = @PartID) < @Quantity
            RAISERROR('Недостаточно запчастей на складе', 16, 1);
        DECLARE @Price DECIMAL(10,2) = (SELECT Price FROM Parts WHERE PartID = @PartID);
        INSERT INTO OrderParts (OrderID, PartID, Quantity, PricePerUnit)
        VALUES (@OrderID, @PartID, @Quantity, @Price);
        UPDATE Parts SET QuantityInStock = QuantityInStock - @Quantity WHERE PartID = @PartID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
```

---

### Скрипт 2: Триггеры аудита

> Выполнить вторым — после `01_Create_Database.sql`. Создаёт триггеры и вспомогательную функцию.

```sql
-- ================================================================
-- Файл: 02_Create_Triggers.sql
-- Назначение: Триггеры аудита (журналирование всех изменений в
--             критически важных таблицах) для CarCare 360
-- ================================================================
USE CarCare360;
GO

-- Вспомогательная функция для извлечения первичного ключа
-- (используется во всех триггерах)
CREATE FUNCTION dbo.GetPrimaryKeyValue(@table NVARCHAR(50), @json NVARCHAR(MAX))
RETURNS NVARCHAR(100)
AS
BEGIN
    DECLARE @key NVARCHAR(100);
    -- Для простоты возвращаем первое значение (подходит для одно-ключевых таблиц)
    SELECT @key = [value] FROM OPENJSON(@json) WHERE [type] = 1;
    RETURN @key;
END;
GO

-- ================================================================
-- Триггер на таблицу Orders (заказ-наряды)
-- ================================================================
CREATE TRIGGER trg_Audit_Orders
ON Orders
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Operation CHAR(1) = 
        CASE 
            WHEN EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted) THEN 'U'
            WHEN EXISTS(SELECT * FROM inserted) THEN 'I'
            ELSE 'D'
        END;
    DECLARE @Old NVARCHAR(MAX), @New NVARCHAR(MAX);
    IF @Operation IN ('U','D') 
        SET @Old = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    IF @Operation IN ('I','U')
        SET @New = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    INSERT INTO AuditLog (TableName, Operation, PrimaryKeyValue, ChangedBy, OldValues, NewValues)
    SELECT 'Orders', @Operation, 
           CAST(ISNULL(i.OrderID, d.OrderID) AS NVARCHAR),
           SYSTEM_USER, @Old, @New
    FROM inserted i FULL OUTER JOIN deleted d ON i.OrderID = d.OrderID;
END;
GO

-- ================================================================
-- Триггер на таблицу Cars (автомобили – персональные данные)
-- ================================================================
CREATE TRIGGER trg_Audit_Cars
ON Cars
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Operation CHAR(1) = 
        CASE 
            WHEN EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted) THEN 'U'
            WHEN EXISTS(SELECT * FROM inserted) THEN 'I'
            ELSE 'D'
        END;
    DECLARE @Old NVARCHAR(MAX), @New NVARCHAR(MAX);
    IF @Operation IN ('U','D') 
        SET @Old = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    IF @Operation IN ('I','U')
        SET @New = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    INSERT INTO AuditLog (TableName, Operation, PrimaryKeyValue, ChangedBy, OldValues, NewValues)
    SELECT 'Cars', @Operation,
           CAST(ISNULL(i.CarID, d.CarID) AS NVARCHAR),
           SYSTEM_USER, @Old, @New
    FROM inserted i FULL OUTER JOIN deleted d ON i.CarID = d.CarID;
END;
GO

-- ================================================================
-- Триггер на таблицу Users (пользователи – защита персональных данных)
-- ================================================================
CREATE TRIGGER trg_Audit_Users
ON Users
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Operation CHAR(1) = 
        CASE 
            WHEN EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted) THEN 'U'
            WHEN EXISTS(SELECT * FROM inserted) THEN 'I'
            ELSE 'D'
        END;
    DECLARE @Old NVARCHAR(MAX), @New NVARCHAR(MAX);
    IF @Operation IN ('U','D') 
        SET @Old = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    IF @Operation IN ('I','U')
        SET @New = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    INSERT INTO AuditLog (TableName, Operation, PrimaryKeyValue, ChangedBy, OldValues, NewValues)
    SELECT 'Users', @Operation,
           CAST(ISNULL(i.UserID, d.UserID) AS NVARCHAR),
           SYSTEM_USER, @Old, @New
    FROM inserted i FULL OUTER JOIN deleted d ON i.UserID = d.UserID;
END;
GO
```

---

## 4. Архитектура приложения

Приложение строго следует паттерну **MVVM**:

```
View  ──(DataContext/Binding)──►  ViewModel  ──(EF Core)──►  Model / БД
  ▲                                   │
  └──────────(Commands/Events)────────┘
```

### Структура проекта

```
CarCare360/
├── CarCare360.slnx
├── README.md
└── CarCare360.Desktop/
    ├── App.config                      # Строка подключения к БД
    ├── App.xaml                        # Глобальные ресурсы (кисти, стили)
    ├── App.xaml.cs
    ├── AssemblyInfo.cs
    │
    ├── Models/                         # 14 EF Core-сущностей
    │   ├── Role.cs
    │   ├── User.cs
    │   ├── CarBrand.cs
    │   ├── CarModel.cs
    │   ├── Car.cs
    │   ├── Specialization.cs
    │   ├── Mechanic.cs
    │   ├── ServiceBay.cs
    │   ├── Service.cs
    │   ├── Part.cs
    │   ├── Order.cs
    │   ├── OrderService.cs
    │   ├── OrderPart.cs
    │   └── AuditLog.cs
    │
    ├── Data/
    │   └── CarCareDbContext.cs          # EF Core DbContext
    │
    ├── ViewModels/                      # 23 файла
    │   ├── LoginViewModel.cs
    │   ├── MainViewModel.cs
    │   ├── RegistrationViewModel.cs
    │   ├── ClientsViewModel.cs
    │   ├── ClientEditViewModel.cs
    │   ├── CarsViewModel.cs
    │   ├── CarEditViewModel.cs
    │   ├── OrdersViewModel.cs
    │   ├── OrderCreateViewModel.cs
    │   ├── OrderDetailViewModel.cs
    │   ├── MechanicsViewModel.cs
    │   ├── MechanicEditViewModel.cs
    │   ├── WarehouseViewModel.cs
    │   ├── PartEditViewModel.cs
    │   ├── ReferencesViewModel.cs
    │   ├── BaysRefViewModel.cs
    │   ├── BrandsRefViewModel.cs
    │   ├── ServicesRefViewModel.cs
    │   ├── SpecializationsRefViewModel.cs
    │   ├── ProfileViewModel.cs
    │   ├── AuditViewModel.cs
    │   ├── ReportsViewModel.cs
    │   └── ClientPortalViewModels.cs
    │
    ├── Views/                           # 27 XAML + 27 code-behind
    │   ├── LoginWindow.xaml / .cs
    │   ├── MainWindow.xaml / .cs
    │   ├── RegistrationWindow.xaml / .cs
    │   ├── ClientWindow.xaml / .cs      # Портал клиента
    │   │
    │   ├── ClientsView.xaml / .cs
    │   ├── CarsView.xaml / .cs
    │   ├── OrdersView.xaml / .cs
    │   ├── MechanicsView.xaml / .cs
    │   ├── WarehouseView.xaml / .cs
    │   ├── ReferencesView.xaml / .cs
    │   ├── AuditView.xaml / .cs
    │   ├── ReportsView.xaml / .cs
    │   ├── ProfileView.xaml / .cs
    │   ├── ServicesPublicView.xaml / .cs
    │   │
    │   ├── ClientEditDialog.xaml / .cs
    │   ├── CarEditDialog.xaml / .cs
    │   ├── PartEditDialog.xaml / .cs
    │   ├── MechanicEditDialog.xaml / .cs
    │   ├── OrderCreateDialog.xaml / .cs
    │   ├── OrderDetailDialog.xaml / .cs
    │   │
    │   ├── ClientCarsView.xaml / .cs    # Портал: автомобили клиента
    │   ├── ClientCarAddDialog.xaml / .cs
    │   ├── ClientOrdersView.xaml / .cs  # Портал: заказы клиента
    │   ├── ClientOrderCreateDialog.xaml / .cs
    │   ├── ClientOrderDetailDialog.xaml / .cs
    │   │
    │   ├── ToastNotification.xaml / .cs # Всплывающие уведомления
    │   └── SkeletonLoading.xaml / .cs   # Скелетная загрузка
    │
    ├── Helpers/                         # 13 файлов
    │   ├── BaseViewModel.cs             # INotifyPropertyChanged базовый класс
    │   ├── RelayCommand.cs              # ICommand для MVVM
    │   ├── PasswordBoxHelper.cs         # Attached behavior для привязки пароля
    │   ├── CurrentUser.cs               # Синглтон авторизованного пользователя
    │   ├── DialogHelper.cs              # Управление диалогами
    │   ├── ToastHelper.cs               # Показ toast-уведомлений
    │   ├── DatabaseSeeder.cs            # Начальный посев данных (admin/admin)
    │   ├── UserAvatarStorage.cs         # Хранение аватаров пользователей
    │   ├── RememberLoginHelper.cs       # Сохранение учётных данных "Запомнить меня"
    │   ├── AnimatedContentControl.cs    # Кастомный контрол с анимацией переходов
    │   ├── InverseBoolToVisibilityConverter.cs
    │   ├── AuditOperationConverter.cs   # I/U/D → человекочитаемая строка
    │   └── QuantityToLowStockConverter.cs  # Индикатор низкого остатка
    │
    └── Services/                        # Зарезервировано для бизнес-сервисов
```

**Итого:** ~83 файла (14 моделей + 23 ViewModel + 27×2 XAML/cs + 13 хелперов + 1 DbContext + конфигурация).

### Ключевые принципы

- **`BaseViewModel`** реализует `INotifyPropertyChanged` — все ViewModel наследуют его.
- **`RelayCommand`** — универсальная реализация `ICommand` с поддержкой `CanExecute`.
- **`CurrentUser`** — статический синглтон, хранит данные вошедшего пользователя и его роль.
- **EF Core `DbContext`** конфигурируется из `App.config`, модели генерируются `Scaffold-DbContext`.
- **`DatabaseSeeder`** при первом запуске создаёт тестового пользователя `admin`.

---

## 5. Производительность и временные характеристики

| Метрика | Значение |
|---|---|
| Отклик навигации между разделами | < 100 мс |
| Длительность анимации перехода | 300 мс (Ease-In-Out) |
| Типичный запрос к БД (список записей) | < 200 мс |
| Скелетная загрузка показывается при | > 150 мс ожидания |
| Длительность toast-уведомления | ~3 сек, затем автоскрытие |

### Минимальные системные требования

| Компонент | Минимум |
|---|---|
| ОС | Windows 10 x64 (Build 19041+) или Windows 11 |
| .NET Runtime | .NET 10.0 (Windows Desktop Runtime) |
| СУБД | Microsoft SQL Server 2019 Express |
| ОЗУ | 4 ГБ |
| Дисковое пространство | 200 МБ (без учёта БД) |
| Разрешение экрана | 1280 × 720 и выше |

---

## 6. Инструкция по развёртыванию и запуску

### Шаг 1. Установить необходимое ПО

1. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — для сборки и запуска
2. [Microsoft SQL Server 2019 Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) — экземпляр `SQLEXPRESS`
3. [SQL Server Management Studio](https://aka.ms/ssmsfullsetup) (SSMS) — для выполнения скриптов
4. [Visual Studio 2022](https://visualstudio.microsoft.com/) или [VS Code](https://code.visualstudio.com/) с расширением C#

### Шаг 2. Создать базу данных

Откройте SSMS, подключитесь к `.\SQLEXPRESS` и выполните:

```sql
CREATE DATABASE CarCare360;
```

Затем последовательно выполните оба SQL-скрипта из [раздела 3](#3-база-данных):

1. `01_Create_Database.sql` — таблицы, индексы, процедуры, начальные роли
2. `02_Create_Triggers.sql` — триггеры аудита

### Шаг 3. Проверить строку подключения

Откройте [CarCare360.Desktop/App.config](CarCare360.Desktop/App.config) и убедитесь, что строка подключения указывает на ваш экземпляр:

```xml
<connectionStrings>
  <add name="CarCareDbContext"
       connectionString="Server=.\SQLEXPRESS;Database=CarCare360;Trusted_Connection=True;TrustServerCertificate=True;"
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

Если имя экземпляра отличается — скорректируйте `Server=`.

### Шаг 4. Сгенерировать EF-модели

Модели EF Core создаются из существующей БД командой `Scaffold-DbContext`.

**Вариант A: Package Manager Console (Visual Studio)**

```powershell
Scaffold-DbContext "Name=CarCareDbContext" `
    Microsoft.EntityFrameworkCore.SqlServer `
    -OutputDir Models `
    -ContextDir Data `
    -Context CarCareDbContext `
    -Force
```

**Вариант B: dotnet CLI**

```bash
dotnet ef dbcontext scaffold "Name=CarCareDbContext" \
    Microsoft.EntityFrameworkCore.SqlServer \
    --output-dir Models \
    --context-dir Data \
    --context CarCareDbContext \
    --force
```

### Шаг 5. Исправить OnConfiguring в DbContext

После генерации откройте [CarCare360.Desktop/Data/CarCareDbContext.cs](CarCare360.Desktop/Data/CarCareDbContext.cs) и замените автоматически добавленную хардкодированную строку подключения:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        var connectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["CarCareDbContext"].ConnectionString;
        optionsBuilder.UseSqlServer(connectionString);
    }
}
```

Это гарантирует, что строка подключения берётся из `App.config`, а не зашита в код.

### Шаг 6. Собрать и запустить

```bash
dotnet restore
dotnet build CarCare360.slnx
dotnet run --project CarCare360.Desktop/CarCare360.Desktop.csproj
```

### Шаг 7. Войти в систему

При первом запуске `DatabaseSeeder` автоматически создаёт тестового пользователя:

| Логин | Пароль | Роль |
|---|---|---|
| `admin` | `admin123` | Администратор |

После входа открывается главное окно со всеми восемью модулями.

---

## 7. Безопасность

### Хеширование паролей

Пароли хранятся в виде bcrypt-хеша (библиотека **BCrypt.Net-Next**, work factor 12).  
Поле `PasswordHash BINARY(64)` в таблице `Users`. Проверка пароля:

```csharp
bool valid = BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
```

### Параметризованные запросы

Все операции с БД выполняются через **EF Core** и хранимые процедуры с параметрами — SQL-инъекции исключены на уровне ORM и СУБД.

### RBAC (Role-Based Access Control)

| Роль | Клиенты | Авто | Заказы | Склад | Механики | Справочники | Аудит | Отчёты |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Администратор | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Механик | ✓ | ✓ | ✓ | — | ✓ | — | — | — |
| Клиент | (портал) | (портал) | (портал) | — | — | — | — | — |

Фильтрация меню реализована в `MainViewModel` на основе `CurrentUser.Role`.

### Мягкое удаление

Записи не удаляются физически — вместо этого устанавливается флаг `IsDeleted = 1`. Данные сохраняются для аудита и возможного восстановления.

### Аудит-триггеры

Три AFTER-триггера на таблицах `Users`, `Cars`, `Orders` автоматически записывают в `AuditLog` все операции INSERT, UPDATE, DELETE с JSON-снимками старых и новых значений.

### "Запомнить меня"

`RememberLoginHelper` сохраняет учётные данные в защищённом хранилище Windows. Реализован кастомный чекбокс в тёмном стиле на экране входа.

### Защита от брутфорса

Блокировка по количеству неудачных попыток входа запланирована в следующей итерации.

---

## 8. Диагностика и тестирование

### Сценарии проверки

| Сценарий | Ожидаемый результат |
|---|---|
| Вход с `admin` / `admin` | Открывается главное окно, все 8 разделов видны |
| Вход с учётной записью механика | Скрыты: Склад, Справочники, Аудит, Отчёты |
| Создание клиента → сохранение | Запись появляется в списке, триггер добавляет строку в AuditLog |
| Добавление автомобиля клиенту | Авто видно в профиле клиента и в разделе Автомобили |
| Создание заказа | Статус = «Новый», заказ виден в списке |
| Смена статуса заказа | Статус обновляется, запись попадает в AuditLog |
| Списание запчасти в заказ | `QuantityInStock` уменьшается на указанное количество |
| Попытка списать больше остатка | Хранимая процедура бросает ошибку, транзакция откатывается |
| Удаление клиента | `IsDeleted = 1`, клиент пропадает из списка, но остаётся в БД |

### Нагрузочное тестирование

Для проверки производительности вставьте в БД 1000+ заказов:

```sql
-- Пример генерации тестовых данных
INSERT INTO Orders (CarID, ClientID, ScheduledDate, Status)
SELECT TOP 1000 1, 1, DATEADD(day, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), GETDATE()), 'Новый'
FROM sys.objects CROSS JOIN sys.columns;
```

Убедитесь, что `OrdersView` открывается менее чем за 500 мс и пагинация работает корректно.

### Мониторинг AuditLog

Для проверки работы триггеров выполните в SSMS после любой операции в UI:

```sql
SELECT TOP 20 * FROM AuditLog ORDER BY ChangedAt DESC;
```

---

## 9. Визуальный стиль и анимации

### Цветовая схема

| Назначение | Цвет | HEX |
|---|---|---|
| Фон меню, заголовки, навигация | Тёмно-синий | `#1A237E` |
| Акценты, кнопки, hover, чекмарк | Оранжевый | `#FF6B00` |
| Рабочая область, фон контента | Светло-серый | `#F5F5F5` |
| Текст на тёмном фоне | Белый | `#FFFFFF` |
| Вторичный текст, подписи | Серый | `#AAAAAA` |
| Сообщения об ошибках | Красный | `#D32F2F` |
| Граница неактивных элементов | Тёмно-серый | `#555555` |

### Ресурсы (App.xaml)

Все цвета объявлены как именованные кисти в [CarCare360.Desktop/App.xaml](CarCare360.Desktop/App.xaml):

```xml
<SolidColorBrush x:Key="PrimaryDarkBlueBrush"   Color="#1A237E"/>
<SolidColorBrush x:Key="AccentOrangeBrush"       Color="#FF6B00"/>
<SolidColorBrush x:Key="LightBackgroundBrush"    Color="#F5F5F5"/>
<SolidColorBrush x:Key="ErrorRedBrush"           Color="#D32F2F"/>
```

### Компоненты и анимации

| Компонент | Файл | Описание |
|---|---|---|
| `AnimatedContentControl` | `Helpers/AnimatedContentControl.cs` | Кастомный `ContentControl` с плавными анимированными переходами (300 мс, Ease-In-Out) между View при навигации |
| Лава-лампа (Lava Lamp) | `Views/LoginWindow.xaml` | Анимированный декоративный фон на экране входа — плавающие цветные «капли» |
| `SkeletonLoading` | `Views/SkeletonLoading.xaml` | Серые полосы-заглушки, видимые во время загрузки данных из БД (> 150 мс) |
| `ToastNotification` | `Views/ToastNotification.xaml` | Всплывающие уведомления в углу экрана (успех / ошибка / предупреждение), автоскрытие через ~3 сек |
| `NavigationButtonStyle` | `Views/MainWindow.xaml` | Кастомный стиль кнопок бокового меню с hover-эффектом оранжевого акцента |
| Кастомный чекбокс | `Views/LoginWindow.xaml` | «Запомнить меня» — тёмная рамка, оранжевая галочка из Segoe MDL2 Assets при активации |

### Иконки

Все иконки берутся из шрифта **Segoe MDL2 Assets** (встроен в Windows 10/11) — никаких внешних зависимостей.

---

## 10. Дополнительные сведения

### Статус разработки

- **Десктопное приложение (WPF)** — полностью реализовано: авторизация, все 8 модулей, RBAC, аудит, анимации, клиентский портал
- **REST API (ASP.NET Core)** — реализовано (каталог `CarCare360.Api`): JWT-аутентификация, эндпоинты для мобильного клиента
- **Мобильное приложение (Flutter)** — реализовано (каталог `carcare_mobile`): клиентский портал для самозаписи на сервис
- **Экспорт отчётов в Excel** — запланировано (EPPlus или ClosedXML)

### Учебный контекст

Проект создан как учебная практическая работа (ПП) и демонстрирует:

- Применение паттерна MVVM в WPF на .NET 10
- Работу с Entity Framework Core (Code-First Scaffold из существующей БД)
- Хранимые процедуры и триггеры SQL Server
- Реализацию RBAC
- Безопасное хеширование паролей bcrypt
- Анимации и кастомные компоненты WPF
- Мягкое удаление и ведение журнала аудита

### Перспективы развития

- Push-уведомления при смене статуса заказа (серверная отправка FCM)
- Экспорт отчётов и журнала аудита в CSV/Excel
- Блокировка после N неудачных попыток входа
- Тёмная тема (переключатель в настройках профиля)
