using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Execution.Contexts;
using System.Data;

namespace SapLoVcSharp.Data.Sqlite.Contexts
{
    /// <summary>
    /// SQL-based table resolver that queries real database tables for variant table data.
    /// Each variant table has its own physical table (VT_*) for optimal performance.
    /// </summary>
    public class TableResolver : ITableResolver
    {
        private readonly SqliteDbContext _dbContext;

        public TableResolver(SqliteDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ResolveAsync(
            string tableName,
            Dictionary<string, (object? Value, bool IsOptional)> arguments)
        {
            // Load table metadata
            var table = await _dbContext.VariantTables
                .Include(t => t.Columns.OrderBy(c => c.ColumnIndex))
                .FirstOrDefaultAsync(t => t.Name == tableName);

            if (table == null)
            {
                throw new InvalidOperationException($"Variant table '{tableName}' not found");
            }

            if (!table.IsTableCreated)
            {
                throw new InvalidOperationException($"Physical table for '{tableName}' has not been created");
            }

            // Build WHERE clause for required (non-optional) arguments
            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            foreach (var (characteristicName, (value, isOptional)) in arguments)
            {
                // Optional parameters (marked with ?=) are OUTPUT parameters only
                if (isOptional) continue;

                whereClauses.Add($"[{characteristicName}] = @{characteristicName}");
                parameters.Add(new SqliteParameter($"@{characteristicName}", value ?? DBNull.Value));
            }

            // Build and execute query
            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            var sql = $"SELECT COUNT(*) FROM [{table.DatabaseTableName}] {whereClause}";

            var connection = _dbContext.Database.GetDbConnection();
            await EnsureConnectionOpenAsync(connection);

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddRange(parameters.ToArray());

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<List<Dictionary<string, string>>> GetMatchingRowsAsync(
            string tableName,
            Dictionary<string, (object? Value, bool IsOptional)> arguments)
        {
            // Load table metadata
            var table = await _dbContext.VariantTables
                .Include(t => t.Columns.OrderBy(c => c.ColumnIndex))
                .FirstOrDefaultAsync(t => t.Name == tableName);

            if (table == null)
            {
                throw new InvalidOperationException($"Variant table '{tableName}' not found");
            }

            if (!table.IsTableCreated)
            {
                throw new InvalidOperationException($"Physical table for '{tableName}' has not been created");
            }

            // Build WHERE clause for required (non-optional) arguments
            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            foreach (var (characteristicName, (value, isOptional)) in arguments)
            {
                // Optional parameters (marked with ?=) are OUTPUT parameters only
                if (isOptional) continue;

                whereClauses.Add($"[{characteristicName}] = @{characteristicName}");
                parameters.Add(new SqliteParameter($"@{characteristicName}", value ?? DBNull.Value));
            }

            // Build query to select all columns
            var columnNames = string.Join(", ", table.Columns.Select(c => $"[{c.CharacteristicName}]"));
            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            var sql = $"SELECT {columnNames} FROM [{table.DatabaseTableName}] {whereClause}";

            var connection = _dbContext.Database.GetDbConnection();
            await EnsureConnectionOpenAsync(connection);

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddRange(parameters.ToArray());

            var matchingRows = new List<Dictionary<string, string>>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var rowData = new Dictionary<string, string>();
                foreach (var column in table.Columns)
                {
                    var ordinal = reader.GetOrdinal(column.CharacteristicName);
                    rowData[column.CharacteristicName] = reader.IsDBNull(ordinal)
                        ? string.Empty
                        : reader.GetString(ordinal);
                }
                matchingRows.Add(rowData);
            }

            return matchingRows;
        }

        public async Task<List<Dictionary<string, string>>> GetMatchingRowsWithAllowedValuesAsync(
            string tableName,
            Dictionary<string, List<string>> allowedValues)
        {
            // Load table metadata
            var table = await _dbContext.VariantTables
                .Include(t => t.Columns.OrderBy(c => c.ColumnIndex))
                .FirstOrDefaultAsync(t => t.Name == tableName);

            if (table == null)
            {
                throw new InvalidOperationException($"Variant table '{tableName}' not found");
            }

            if (!table.IsTableCreated)
            {
                throw new InvalidOperationException($"Physical table for '{tableName}' has not been created");
            }

            // Build WHERE clause for characteristics with allowed value sets
            // For each characteristic, use IN clause: [COLUMN] IN (@value1, @value2, ...)
            var whereClauses = new List<string>();
            var parameters = new List<SqliteParameter>();
            int paramIndex = 0;

            foreach (var (characteristicName, values) in allowedValues)
            {
                if (values == null || !values.Any()) continue;

                // Build IN clause with parameterized values
                var inParams = new List<string>();
                foreach (var value in values)
                {
                    var paramName = $"@p{paramIndex++}";
                    inParams.Add(paramName);
                    parameters.Add(new SqliteParameter(paramName, value));
                }

                // Use COLLATE NOCASE for case-insensitive comparison (SAP standard)
                whereClauses.Add($"[{characteristicName}] COLLATE NOCASE IN ({string.Join(", ", inParams)})");
            }

            // Build query to select all columns
            var columnNames = string.Join(", ", table.Columns.Select(c => $"[{c.CharacteristicName}]"));
            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            var sql = $"SELECT {columnNames} FROM [{table.DatabaseTableName}] {whereClause}";

            var connection = _dbContext.Database.GetDbConnection();
            await EnsureConnectionOpenAsync(connection);

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddRange(parameters.ToArray());

            var matchingRows = new List<Dictionary<string, string>>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var rowData = new Dictionary<string, string>();
                foreach (var column in table.Columns)
                {
                    var ordinal = reader.GetOrdinal(column.CharacteristicName);
                    rowData[column.CharacteristicName] = reader.IsDBNull(ordinal)
                        ? string.Empty
                        : reader.GetString(ordinal);
                }
                matchingRows.Add(rowData);
            }

            return matchingRows;
        }

        /// <summary>
        /// Ensures database connection is open (handles both new and existing connections).
        /// </summary>
        private static async Task EnsureConnectionOpenAsync(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await ((SqliteConnection)connection).OpenAsync();
            }
        }
    }
}
