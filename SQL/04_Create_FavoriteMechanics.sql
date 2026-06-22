/* ============================================================================
   CarCare360 — Скрипт 4: таблица FavoriteMechanics (Изменение №2, Доработка 3)
   ----------------------------------------------------------------------------
   Назначение: избранные механики клиента (мобильное приложение). Клиент
   отмечает понравившихся механиков и видит их первыми при выборе.

   Это новая таблица. Существующие таблицы, хранимые процедуры и триггеры из
   01_Create_Database.sql / 02_Create_Triggers.sql / 03_Create_RefreshTokens.sql
   НЕ изменяются.

   Применение (PowerShell):
     sqlcmd -S .\SQLEXPRESS -d CarCare360 -E -i "04_Create_FavoriteMechanics.sql"
   ============================================================================ */

USE CarCare360;
GO

IF OBJECT_ID('dbo.FavoriteMechanics', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FavoriteMechanics
    (
        FavoriteID  INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_FavoriteMechanics PRIMARY KEY,
        UserID      INT NOT NULL,
        MechanicID  INT NOT NULL,
        AddedAt     DATETIME2 NOT NULL
            CONSTRAINT DF_FavoriteMechanics_AddedAt DEFAULT (GETDATE()),
        -- Избранное — личные данные клиента: при удалении пользователя удаляем.
        CONSTRAINT FK_FavoriteMechanics_Users
            FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID) ON DELETE CASCADE,
        -- Механики не удаляются жёстко (деактивируются через Users.IsActive),
        -- поэтому каскад по MechanicID не нужен.
        CONSTRAINT FK_FavoriteMechanics_Mechanics
            FOREIGN KEY (MechanicID) REFERENCES dbo.Mechanics(MechanicID),
        -- Один механик может быть в избранном клиента только один раз.
        CONSTRAINT UQ_FavoriteMechanics UNIQUE (UserID, MechanicID)
    );

    PRINT 'Таблица FavoriteMechanics успешно создана.';
END
ELSE
BEGIN
    PRINT 'Таблица FavoriteMechanics уже существует — пропуск.';
END
GO
