using System.Data;
using Microsoft.Data.SqlClient;

namespace DPDP_Api.Data;

public sealed class Db
{
    private readonly string _connectionString;

    public Db(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing.");
    }

    public SqlParameter P(string name, object? value)
    {
        return new SqlParameter(
            name,
            value ?? DBNull.Value);
    }

    public async Task<List<Dictionary<string, object?>>> RowsAsync(string procedure, IEnumerable<SqlParameter>? parameters, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, object?>>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(procedure, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        if (parameters != null)
        {
            command.Parameters.AddRange(
                parameters.ToArray());
        }

        await connection.OpenAsync(ct);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var row =
                new Dictionary<string, object?>(
                    StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] =
                    reader.IsDBNull(i)
                        ? null
                        : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public async Task<int> ScalarIntAsync(
        string procedure,
        IEnumerable<SqlParameter>? parameters,
        CancellationToken ct)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(procedure, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        if (parameters != null)
        {
            command.Parameters.AddRange(
                parameters.ToArray());
        }

        await connection.OpenAsync(ct);

        var value =
            await command.ExecuteScalarAsync(ct);

        return Convert.ToInt32(value);
    }

    public async Task<object?> ScalarAsync(
        string procedure,
        IEnumerable<SqlParameter>? parameters,
        CancellationToken ct)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(procedure, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        if (parameters != null)
        {
            command.Parameters.AddRange(
                parameters.ToArray());
        }

        await connection.OpenAsync(ct);

        return await command.ExecuteScalarAsync(ct);
    }

    public async Task ExecuteAsync(
        string procedure,
        IEnumerable<SqlParameter>? parameters,
        CancellationToken ct = default)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(procedure, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        if (parameters != null)
        {
            command.Parameters.AddRange(
                parameters.ToArray());
        }

        await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }
}