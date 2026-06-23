/* ============================================================================
   CarCare360 — Скрипт 5: таблица LoginAuditLog (Изменение №2, Доработка 4)
   ----------------------------------------------------------------------------
   Назначение: аудит входа пользователей в десктопное приложение. Фиксирует
   каждую попытку входа (успех/провал) и время выхода. ПАРОЛЬ НЕ ХРАНИТСЯ —
   только логин, результат и время.

   Это новая таблица. Существующие таблицы, процедуры и триггеры из скриптов
   01..04 НЕ изменяются.

   Применение (PowerShell, к рабочей БД десктопа):
     sqlcmd -S .\SQLEXPRESS -d CarCare360 -E -i "05_Create_LoginAuditLog.sql"
   ============================================================================ */

USE CarCare360;
GO

IF OBJECT_ID('dbo.LoginAuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginAuditLog
    (
        LogID    BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_LoginAuditLog PRIMARY KEY,
        -- Введённый логin фиксируется всегда, даже если пользователь не найден.
        Login    NVARCHAR(100) NOT NULL,
        -- NULL, если введённый логин не существует в системе.
        UserID   INT NULL,
        -- 'S' — успешный вход, 'F' — неудачный. Пароль НЕ хранится.
        Result   CHAR(1) NOT NULL
            CONSTRAINT CK_LoginAuditLog_Result CHECK (Result IN ('S','F')),
        LoginAt  DATETIME2 NOT NULL
            CONSTRAINT DF_LoginAuditLog_LoginAt DEFAULT (GETDATE()),
        -- Заполняется при выходе/закрытии; NULL — активная сессия либо
        -- аварийное завершение приложения.
        LogoutAt DATETIME2 NULL,
        -- Без каскада: пользователи удаляются мягко, запись аудита сохраняется.
        CONSTRAINT FK_LoginAuditLog_Users
            FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );

    CREATE NONCLUSTERED INDEX IX_LoginAuditLog_Login   ON dbo.LoginAuditLog(Login);
    CREATE NONCLUSTERED INDEX IX_LoginAuditLog_LoginAt ON dbo.LoginAuditLog(LoginAt);

    PRINT 'Таблица LoginAuditLog успешно создана.';
END
ELSE
BEGIN
    PRINT 'Таблица LoginAuditLog уже существует — пропуск.';
END
GO
