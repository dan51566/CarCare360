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
