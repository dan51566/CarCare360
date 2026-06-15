using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarCare360.Api.Data;

/// <summary>
/// Вызов хранимых процедур БД через соединение EF Core.
/// Используется ADO.NET (SqlCommand) поверх соединения контекста, что:
///  — корректно работает с таблицами, имеющими триггеры (нет OUTPUT-clause);
///  — позволяет читать скалярный результат (SCOPE_IDENTITY) независимо от имени столбца;
///  — использует параметры SqlParameter — защита от SQL-инъекций.
/// </summary>
public static class StoredProcedureRunner
{
    /// <summary>
    /// Выполняет процедуру и возвращает целочисленный скаляр (первый столбец первой строки),
    /// например NewUserID / NewOrderID из SELECT SCOPE_IDENTITY().
    /// </summary>
    public static async Task<int> ExecuteScalarIntAsync(
        this CarCareDbContext db, string procedure, params SqlParameter[] parameters)
    {
        var result = await ExecuteScalarAsync(db, procedure, parameters);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    /// <summary>
    /// Выполняет процедуру без чтения результата (INSERT/UPDATE внутри SP).
    /// </summary>
    public static async Task ExecuteNonQueryAsync(
        this CarCareDbContext db, string procedure, params SqlParameter[] parameters)
        => await ExecuteScalarAsync(db, procedure, parameters);

    private static async Task<object?> ExecuteScalarAsync(
        CarCareDbContext db, string procedure, SqlParameter[] parameters)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
            return await command.ExecuteScalarAsync();
        }
        finally
        {
            // Закрываем соединение только если открыли его здесь
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
