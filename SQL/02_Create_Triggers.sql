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
