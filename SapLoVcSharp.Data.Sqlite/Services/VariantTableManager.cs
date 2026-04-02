using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Data.Models;

namespace SapLoVcSharp.Data.Sqlite.Services
{
    /// <summary>
    /// Manages dynamic creation and population of variant table physical database tables.
    /// Each variant table gets its own SQL table for optimal query performance.
    /// </summary>
    public class VariantTableManager
    {
        private readonly SqliteDbContext _context;

        public VariantTableManager(SqliteDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates or updates a variant table in the database.
        /// 1. Creates metadata entry (VariantTableEntity)
        /// 2. Creates physical SQL table (VT_[TableName])
        /// 3. Inserts row data into physical table
        /// </summary>
        public async Task CreateTableAsync(
            string tableName,
            string description,
            string[] columnNames,
            string[] sqlDataTypes,
            bool[] isKeyColumns,
            string[][] rowData)
        {
            if (columnNames.Length != sqlDataTypes.Length || columnNames.Length != isKeyColumns.Length)
            {
                throw new ArgumentException("Column arrays must have the same length");
            }

            var databaseTableName = GetDatabaseTableName(tableName);

            // 1. Create or update metadata
            var table = await _context.VariantTables
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.Name == tableName);

            if (table == null)
            {
                table = new VariantTableEntity
                {
                    Name = tableName,
                    Description = description,
                    DatabaseTableName = databaseTableName,
                    IsTableCreated = false,
                    RowCount = 0
                };

                // Add columns
                for (int i = 0; i < columnNames.Length; i++)
                {
                    table.Columns.Add(new VariantTableColumnEntity
                    {
                        TableName = tableName,
                        ColumnIndex = i,
                        CharacteristicName = columnNames[i],
                        SqlDataType = sqlDataTypes[i],
                        IsKeyColumn = isKeyColumns[i]
                    });
                }

                _context.VariantTables.Add(table);
            }
            else
            {
                // Update existing metadata
                table.Description = description;
                table.UpdatedAt = DateTime.UtcNow;

                // Check if columns have changed
                var existingColumns = table.Columns.OrderBy(c => c.ColumnIndex).ToList();
                var columnsChanged = existingColumns.Count != columnNames.Length ||
                    !existingColumns.Select((c, i) => c.CharacteristicName == columnNames[i] && c.ColumnIndex == i)
                        .All(match => match);

                if (columnsChanged)
                {
                    // Remove existing columns
                    _context.VariantTableColumns.RemoveRange(table.Columns);

                    // Add new columns with updated order
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        table.Columns.Add(new VariantTableColumnEntity
                        {
                            TableName = tableName,
                            ColumnIndex = i,
                            CharacteristicName = columnNames[i],
                            SqlDataType = sqlDataTypes[i],
                            IsKeyColumn = isKeyColumns[i]
                        });
                    }

                    // Need to recreate physical table if it exists
                    if (table.IsTableCreated)
                    {
                        // Drop old table
                        var dropSql = $"DROP TABLE IF EXISTS [{databaseTableName}]";
                        await _context.Database.ExecuteSqlRawAsync(dropSql);
                        table.IsTableCreated = false;
                    }
                }
                else
                {
                    // Just update isKeyColumn flags if columns haven't changed structurally
                    for (int i = 0; i < existingColumns.Count; i++)
                    {
                        existingColumns[i].IsKeyColumn = isKeyColumns[i];
                    }
                }
            }

            await _context.SaveChangesAsync();

            // 2. Create physical table if not exists
            if (!table.IsTableCreated)
            {
                await CreatePhysicalTableAsync(databaseTableName, table.Columns.OrderBy(c => c.ColumnIndex).ToList());
                table.IsTableCreated = true;
                await _context.SaveChangesAsync();
            }

            // 3. Clear existing data and insert new rows
            await ClearTableDataAsync(databaseTableName);
            await InsertRowsAsync(databaseTableName, table.Columns.OrderBy(c => c.ColumnIndex).ToList(), rowData);

            // 4. Update row count
            table.RowCount = rowData.Length;
            table.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Drops the physical table and removes metadata.
        /// </summary>
        public async Task DropTableAsync(string tableName)
        {
            var table = await _context.VariantTables.FindAsync(tableName);
            if (table == null) return;

            // Drop physical table
            if (table.IsTableCreated)
            {
                var sql = $"DROP TABLE IF EXISTS [{table.DatabaseTableName}]";
                await _context.Database.ExecuteSqlRawAsync(sql);
            }

            // Remove metadata
            _context.VariantTables.Remove(table);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the database table name for a variant table (prefixed with VT_).
        /// </summary>
        private static string GetDatabaseTableName(string tableName)
        {
            // Sanitize table name and prefix with VT_
            var sanitized = tableName.Replace("-", "_").Replace(" ", "_");
            return $"VT_{sanitized}";
        }

        /// <summary>
        /// Creates the physical SQL table.
        /// </summary>
        private async Task CreatePhysicalTableAsync(string databaseTableName, List<VariantTableColumnEntity> columns)
        {
            // Build CREATE TABLE statement
            var columnDefinitions = columns.Select(c =>
                $"[{c.CharacteristicName}] {c.SqlDataType} NOT NULL"
            );

            var sql = $@"
                CREATE TABLE [{databaseTableName}] (
                    [_RowId] INTEGER PRIMARY KEY AUTOINCREMENT,
                    {string.Join(",\n                    ", columnDefinitions)}
                )";

            await _context.Database.ExecuteSqlRawAsync(sql);

            // Create indexes on key columns for better query performance
            foreach (var keyColumn in columns.Where(c => c.IsKeyColumn))
            {
                var indexName = $"IX_{databaseTableName}_{keyColumn.CharacteristicName}";
                var indexSql = $@"
                    CREATE INDEX [{indexName}]
                    ON [{databaseTableName}] ([{keyColumn.CharacteristicName}])";
                await _context.Database.ExecuteSqlRawAsync(indexSql);
            }
        }

        /// <summary>
        /// Clears all data from the physical table.
        /// </summary>
        private async Task ClearTableDataAsync(string databaseTableName)
        {
            var sql = $"DELETE FROM [{databaseTableName}]";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Inserts rows into the physical table.
        /// </summary>
        private async Task InsertRowsAsync(
            string databaseTableName,
            List<VariantTableColumnEntity> columns,
            string[][] rowData)
        {
            if (rowData.Length == 0) return;

            // Build column list
            var columnNames = string.Join(", ", columns.Select(c => $"[{c.CharacteristicName}]"));

            // Insert rows in batches for performance
            const int batchSize = 100;
            for (int i = 0; i < rowData.Length; i += batchSize)
            {
                var batch = rowData.Skip(i).Take(batchSize).ToArray();

                // Build VALUES clauses
                var valuesClauses = batch.Select(row =>
                {
                    if (row.Length != columns.Count)
                    {
                        throw new ArgumentException($"Row has {row.Length} values but expected {columns.Count}");
                    }

                    var values = string.Join(", ", row.Select(v => $"'{EscapeSql(v)}'"));
                    return $"({values})";
                });

                var sql = $@"
                    INSERT INTO [{databaseTableName}] ({columnNames})
                    VALUES {string.Join(",\n                    ", valuesClauses)}";

                await _context.Database.ExecuteSqlRawAsync(sql);
            }
        }

        /// <summary>
        /// Escapes single quotes in SQL string literals.
        /// </summary>
        private static string EscapeSql(string value)
        {
            return value?.Replace("'", "''") ?? string.Empty;
        }
    }
}
