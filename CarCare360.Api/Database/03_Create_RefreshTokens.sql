/* ============================================================================
   CarCare360 — Скрипт 3: таблица RefreshTokens
   ----------------------------------------------------------------------------
   Назначение: хранение refresh-токенов для JWT-аутентификации серверного API.
   Это ЕДИНСТВЕННАЯ новая таблица, добавляемая API. Существующие 14 таблиц,
   хранимые процедуры и триггеры НЕ изменяются.

   Применение (PowerShell):
     sqlcmd -S .\SQLEXPRESS -d CarCare360 -E -i "03_Create_RefreshTokens.sql"
   ============================================================================ */

USE CarCare360;
GO

IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        TokenID     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserID      INT            NOT NULL,
        Token       NVARCHAR(200)  NOT NULL,
        ExpiresAt   DATETIME2      NOT NULL,
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (GETUTCDATE()),
        RevokedAt   DATETIME2      NULL,
        CONSTRAINT FK_RefreshTokens_Users
            FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID) ON DELETE CASCADE
    );

    -- Уникальный индекс по значению токена (быстрый поиск при обновлении/отзыве)
    CREATE UNIQUE NONCLUSTERED INDEX IX_RefreshTokens_Token
        ON dbo.RefreshTokens(Token);

    -- Индекс по пользователю (массовый отзыв токенов пользователя)
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserID
        ON dbo.RefreshTokens(UserID);

    PRINT 'Таблица RefreshTokens успешно создана.';
END
ELSE
BEGIN
    PRINT 'Таблица RefreshTokens уже существует — пропуск.';
END
GO
