using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Api.Models;
using SapLoVcSharp.Data.Sqlite;
using SapLoVcSharp.Data.Sqlite.Services;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Data.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<SqliteDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=saplo-vc.db"));

// Add application services
builder.Services.AddScoped<ExecutionService>();
builder.Services.AddScoped<VariantTableManager>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
    context.Database.Migrate();
}

// ============================================================================
// CONFIGURATION EXECUTION ENDPOINTS
// ============================================================================

app.MapPost("/api/configurations/{materialNumber}/execute", async (
    string materialNumber,
    Dictionary<string, object?>? initialValues,
    SqliteDbContext context,
    ExecutionService executionService) =>
{
    var material = await context.Materials
        .Include(m => m.ConfigurationProfile)
        .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

    if (material == null)
        return Results.NotFound(new { error = $"Material '{materialNumber}' not found" });

    if (material.ConfigurationProfile == null)
        return Results.BadRequest(new { error = $"Material '{materialNumber}' has no configuration profile" });

    var result = await executionService.ExecuteConfigurationAsync(materialNumber, initialValues);

    // Convert action logs from service to API execution log entries
    var executionLog = result.ActionLogs.Select(log => new ExecutionLogEntry
    {
        Cycle = log.Cycle,
        ActionType = log.ActionType,
        Source = log.Source,
        CharacteristicName = log.CharacteristicName,
        OldValue = log.OldValue,
        NewValue = log.NewValue,
        Description = log.Description
    }).ToList();

    // Build dependency execution summaries (deduplicated by name)
    var dependencyExecutions = result.ExecutionLogs
        .GroupBy(log => log.DependencyName)
        .Select(g => new DependencyExecutionSummary
        {
            DependencyName = g.Key,
            DependencyType = g.First().DependencyType,
            Success = g.All(log => log.Success),
            IsConsistent = g.All(log => log.IsConsistent),
            TablesUsed = g.SelectMany(log => log.TablesUsed).Distinct().ToList()
        })
        .ToList();

    return Results.Ok(new ConfigurationResultDto
    {
        MaterialNumber = result.MaterialNumber,
        Success = result.Success,
        IsComplete = result.IsComplete,
        IsStable = result.IsStable,
        CyclesExecuted = result.CyclesExecuted,
        DurationMs = result.DurationMs,
        FinalValues = result.FinalValues.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString()),
        AllowedValues = result.FinalAllowedValues,
        Errors = result.ExecutionLogs
            .Where(log => !log.Success)
            .Select(log => log.ErrorDetails)
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList(),
        ExecutionLog = executionLog,
        DependencyExecutions = dependencyExecutions
    });
})
.WithName("ExecuteConfiguration")
.WithTags("Configuration")
.Produces<ConfigurationResultDto>(200)
.Produces(404)
.Produces(400);

// ============================================================================
// MATERIAL ENDPOINTS
// ============================================================================

app.MapGet("/api/materials", async (SqliteDbContext context) =>
{
    var materials = await context.Materials
        .Include(m => m.ClassAssignments)
        .ThenInclude(ca => ca.Class)
        .ToListAsync();

    return Results.Ok(materials.Select(m => new MaterialDto
    {
        MaterialNumber = m.MaterialNumber,
        Description = m.Description,
        Status = m.Status,
        Classes = m.ClassAssignments.Select(ca => ca.ClassName).ToList()
    }));
})
.WithName("GetMaterials")
.WithTags("Materials")
.Produces<List<MaterialDto>>(200);

app.MapGet("/api/materials/{materialNumber}", async (
    string materialNumber,
    SqliteDbContext context) =>
{
    var material = await context.Materials
        .Include(m => m.ClassAssignments)
        .ThenInclude(ca => ca.Class)
        .ThenInclude(c => c.Characteristics)
        .ThenInclude(ch => ch.Values)
        .Include(m => m.ConfigurationProfile)
        .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

    if (material == null)
        return Results.NotFound(new { error = $"Material '{materialNumber}' not found" });

    return Results.Ok(new MaterialDetailDto
    {
        MaterialNumber = material.MaterialNumber,
        Description = material.Description,
        Status = material.Status,
        HasConfigurationProfile = material.ConfigurationProfile != null,
        Classes = material.ClassAssignments.Select(ca => ca.ClassName).ToList(),
        Characteristics = material.ClassAssignments
            .SelectMany(ca => ca.Class.Characteristics.Select(ch => new { Characteristic = ch, ClassName = ca.ClassName }))
            .OrderBy(x => x.Characteristic.SortOrder)
            .ThenBy(x => x.Characteristic.Name)
            .Select(x => new CharacteristicDto
            {
                Name = x.Characteristic.Name,
                ClassName = x.ClassName,
                Description = x.Characteristic.Description,
                LongDescription = x.Characteristic.LongDescription,
                DataType = x.Characteristic.DataType,
                Length = x.Characteristic.Length,
                IsRequired = x.Characteristic.IsRequired,
                IsRestrictable = x.Characteristic.IsRestrictable,
                IsHidden = x.Characteristic.IsHidden,
                SortOrder = x.Characteristic.SortOrder,
                AllowedValues = x.Characteristic.Values.Select(v => new CharacteristicValueDto
                {
                    Value = v.Value,
                    Description = v.Description,
                    LongDescription = v.LongDescription,
                    IsDefault = v.IsDefault
                }).ToList()
            }).ToList()
    });
})
.WithName("GetMaterial")
.WithTags("Materials")
.Produces<MaterialDetailDto>(200)
.Produces(404);

// ============================================================================
// VARIANT TABLE ENDPOINTS
// ============================================================================

app.MapGet("/api/variant-tables", async (SqliteDbContext context) =>
{
    var tables = await context.VariantTables
        .Include(t => t.Columns)
        .ToListAsync();

    return Results.Ok(tables.Select(t => new VariantTableDto
    {
        Name = t.Name,
        Description = t.Description,
        Status = t.Status,
        DatabaseTableName = t.DatabaseTableName,
        RowCount = t.RowCount,
        IsTableCreated = t.IsTableCreated,
        Columns = t.Columns.OrderBy(c => c.ColumnIndex).Select(c => new VariantTableColumnDto
        {
            CharacteristicName = c.CharacteristicName,
            SqlDataType = c.SqlDataType,
            IsKeyColumn = c.IsKeyColumn
        }).ToList()
    }));
})
.WithName("GetVariantTables")
.WithTags("VariantTables")
.Produces<List<VariantTableDto>>(200);

app.MapPost("/api/variant-tables", async (
    CreateVariantTableRequest request,
    SqliteDbContext context,
    VariantTableManager tableManager) =>
{
    try
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Table name is required" });

        if (request.Columns == null || !request.Columns.Any())
            return Results.BadRequest(new { error = "At least one column is required" });

        // Enforce uppercase for table name
        var tableName = request.Name.ToUpperInvariant();

        // Rows are optional - table can be created empty
        var rows = request.Rows ?? new List<string[]>();

        // Create table
        await tableManager.CreateTableAsync(
            tableName,
            request.Description ?? string.Empty,
            request.Columns.Select(c => c.CharacteristicName.ToUpperInvariant()).ToArray(),
            request.Columns.Select(c => c.SqlDataType ?? "VARCHAR(255)").ToArray(),
            request.Columns.Select(c => c.IsKeyColumn).ToArray(),
            rows.ToArray()
        );

        return Results.Created($"/api/variant-tables/{tableName}",
            new { message = $"Variant table '{tableName}' created successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateVariantTable")
.WithTags("VariantTables")
.Produces(201)
.Produces(400)
.Produces(500);

app.MapPut("/api/variant-tables/{tableName}", async (
    string tableName,
    CreateVariantTableRequest request,
    SqliteDbContext context,
    VariantTableManager tableManager) =>
{
    try
    {
        var existingTable = await context.VariantTables
            .FirstOrDefaultAsync(t => t.Name == tableName);

        if (existingTable == null)
            return Results.NotFound(new { error = $"Variant table '{tableName}' not found" });

        // Update status if provided
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            existingTable.Status = request.Status;
            existingTable.UpdatedAt = DateTime.UtcNow;
        }

        // Update description if provided
        if (request.Description != null)
        {
            existingTable.Description = request.Description;
            existingTable.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // Update uses the same create method which handles updates
        // Enforce uppercase for column names
        await tableManager.CreateTableAsync(
            tableName,
            existingTable.Description,
            request.Columns.Select(c => c.CharacteristicName.ToUpperInvariant()).ToArray(),
            request.Columns.Select(c => c.SqlDataType ?? "VARCHAR(255)").ToArray(),
            request.Columns.Select(c => c.IsKeyColumn).ToArray(),
            request.Rows.ToArray()
        );

        return Results.Ok(new { message = $"Variant table '{tableName}' updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UpdateVariantTable")
.WithTags("VariantTables")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapDelete("/api/variant-tables/{tableName}", async (
    string tableName,
    SqliteDbContext context,
    VariantTableManager tableManager) =>
{
    try
    {
        var table = await context.VariantTables
            .FirstOrDefaultAsync(t => t.Name == tableName);

        if (table == null)
            return Results.NotFound(new { error = $"Variant table '{tableName}' not found" });

        await tableManager.DropTableAsync(tableName);

        return Results.Ok(new { message = $"Variant table '{tableName}' deleted successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("DeleteVariantTable")
.WithTags("VariantTables")
.Produces(200)
.Produces(404)
.Produces(500);

// Get variant table data (rows)
app.MapGet("/api/variant-tables/{tableName}/data", async (
    string tableName,
    SqliteDbContext context) =>
{
    try
    {
        var table = await context.VariantTables
            .Include(t => t.Columns)
            .FirstOrDefaultAsync(t => t.Name == tableName);

        if (table == null)
            return Results.NotFound(new { error = $"Variant table '{tableName}' not found" });

        if (!table.IsTableCreated)
            return Results.Ok(new VariantTableDataDto
            {
                TableName = tableName,
                Columns = table.Columns.OrderBy(c => c.ColumnIndex)
                    .Select(c => c.CharacteristicName).ToList(),
                Rows = new List<string[]>()
            });

        // Query the physical table
        var columnNames = table.Columns.OrderBy(c => c.ColumnIndex)
            .Select(c => c.CharacteristicName).ToList();

        var rows = new List<string[]>();

        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT {string.Join(", ", columnNames.Select(c => $"[{c}]"))} FROM [{table.DatabaseTableName}] ORDER BY _RowId";

        await context.Database.OpenConnectionAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new string[columnNames.Count];
            for (int i = 0; i < columnNames.Count; i++)
            {
                row[i] = reader.IsDBNull(i) ? string.Empty : reader.GetString(i);
            }
            rows.Add(row);
        }

        return Results.Ok(new VariantTableDataDto
        {
            TableName = tableName,
            Columns = columnNames,
            Rows = rows
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetVariantTableData")
.WithTags("VariantTables")
.Produces<VariantTableDataDto>(200)
.Produces(404)
.Produces(500);

// Upload variant table data (CSV/TSV content)
app.MapPost("/api/variant-tables/{tableName}/upload", async (
    string tableName,
    UploadVariantTableDataRequest request,
    SqliteDbContext context,
    VariantTableManager tableManager) =>
{
    try
    {
        var table = await context.VariantTables
            .Include(t => t.Columns)
            .FirstOrDefaultAsync(t => t.Name == tableName);

        if (table == null)
            return Results.NotFound(new { error = $"Variant table '{tableName}' not found" });

        // Parse the content
        var separator = request.Separator ?? "\t";
        var lines = request.Content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            return Results.BadRequest(new { error = "No data found in upload" });

        // Check if first row is header
        var startRow = 0;
        if (request.HasHeader)
        {
            startRow = 1;
        }

        var rows = new List<string[]>();
        var expectedColumns = table.Columns.Count;

        for (int i = startRow; i < lines.Length; i++)
        {
            var values = lines[i].Split(new[] { separator }, StringSplitOptions.None);
            if (values.Length != expectedColumns)
            {
                return Results.BadRequest(new { error = $"Row {i + 1} has {values.Length} columns, expected {expectedColumns}" });
            }
            rows.Add(values);
        }

        // Update the table using the manager
        await tableManager.CreateTableAsync(
            tableName,
            table.Description,
            table.Columns.OrderBy(c => c.ColumnIndex).Select(c => c.CharacteristicName).ToArray(),
            table.Columns.OrderBy(c => c.ColumnIndex).Select(c => c.SqlDataType).ToArray(),
            table.Columns.OrderBy(c => c.ColumnIndex).Select(c => c.IsKeyColumn).ToArray(),
            rows.ToArray()
        );

        return Results.Ok(new { message = $"Uploaded {rows.Count} rows to variant table '{tableName}'" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UploadVariantTableData")
.WithTags("VariantTables")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(500);

// ============================================================================
// MATERIAL CREATION AND MODIFICATION ENDPOINTS
// ============================================================================

app.MapPost("/api/materials", async (
    CreateMaterialRequest request,
    SqliteDbContext context) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.MaterialNumber))
            return Results.BadRequest(new { error = "Material number is required" });

        var existing = await context.Materials
            .FirstOrDefaultAsync(m => m.MaterialNumber == request.MaterialNumber);

        if (existing != null)
            return Results.Conflict(new { error = $"Material '{request.MaterialNumber}' already exists" });

        var material = new SapLoVcSharp.Data.Models.MaterialEntity
        {
            MaterialNumber = request.MaterialNumber,
            Description = request.Description ?? string.Empty,
            Status = request.Status ?? "Active"
        };

        context.Materials.Add(material);
        await context.SaveChangesAsync();

        return Results.Created($"/api/materials/{material.MaterialNumber}", new
        {
            message = $"Material '{material.MaterialNumber}' created successfully",
            materialNumber = material.MaterialNumber
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateMaterial")
.WithTags("Materials")
.Produces(201)
.Produces(400)
.Produces(409)
.Produces(500);

app.MapPut("/api/materials/{materialNumber}", async (
    string materialNumber,
    UpdateMaterialRequest request,
    SqliteDbContext context) =>
{
    var material = await context.Materials
        .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

    if (material == null)
        return Results.NotFound(new { error = $"Material '{materialNumber}' not found" });

    if (!string.IsNullOrWhiteSpace(request.Description))
        material.Description = request.Description;

    if (!string.IsNullOrWhiteSpace(request.Status))
        material.Status = request.Status;

    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Material '{materialNumber}' updated successfully" });
})
.WithName("UpdateMaterial")
.WithTags("Materials")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapDelete("/api/materials/{materialNumber}", async (
    string materialNumber,
    SqliteDbContext context) =>
{
    var material = await context.Materials
        .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

    if (material == null)
        return Results.NotFound(new { error = $"Material '{materialNumber}' not found" });

    context.Materials.Remove(material);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Material '{materialNumber}' deleted successfully" });
})
.WithName("DeleteMaterial")
.WithTags("Materials")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapPost("/api/materials/{materialNumber}/classes", async (
    string materialNumber,
    AssignClassRequest request,
    SqliteDbContext context) =>
{
    var material = await context.Materials
        .Include(m => m.ClassAssignments)
        .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

    if (material == null)
        return Results.NotFound(new { error = $"Material '{materialNumber}' not found" });

    var classEntity = await context.Classes
        .FirstOrDefaultAsync(c => c.Name == request.ClassName);

    if (classEntity == null)
        return Results.NotFound(new { error = $"Class '{request.ClassName}' not found" });

    var existingAssignment = material.ClassAssignments
        .FirstOrDefault(ca => ca.ClassName == request.ClassName);

    if (existingAssignment != null)
        return Results.Conflict(new { error = $"Class '{request.ClassName}' already assigned to material" });

    material.ClassAssignments.Add(new SapLoVcSharp.Data.Models.MaterialClassAssignmentEntity
    {
        MaterialNumber = materialNumber,
        ClassName = request.ClassName
    });

    await context.SaveChangesAsync();

    return Results.Created($"/api/materials/{materialNumber}/classes/{request.ClassName}",
        new { message = $"Class '{request.ClassName}' assigned to material '{materialNumber}'" });
})
.WithName("AssignClassToMaterial")
.WithTags("Materials")
.Produces(201)
.Produces(404)
.Produces(409)
.Produces(500);

app.MapDelete("/api/materials/{materialNumber}/classes/{className}", async (
    string materialNumber,
    string className,
    SqliteDbContext context) =>
{
    var assignment = await context.MaterialClassAssignments
        .FirstOrDefaultAsync(ca => ca.MaterialNumber == materialNumber && ca.ClassName == className);

    if (assignment == null)
        return Results.NotFound(new { error = $"Class assignment not found" });

    context.MaterialClassAssignments.Remove(assignment);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Class '{className}' removed from material '{materialNumber}'" });
})
.WithName("RemoveClassFromMaterial")
.WithTags("Materials")
.Produces(200)
.Produces(404)
.Produces(500);

// ============================================================================
// CLASS ENDPOINTS
// ============================================================================

app.MapGet("/api/classes", async (SqliteDbContext context) =>
{
    var classes = await context.Classes
        .Include(c => c.Characteristics)
        .ToListAsync();

    return Results.Ok(classes.Select(c => new ClassDto
    {
        Name = c.Name,
        ClassType = c.ClassType,
        Description = c.Description,
        Status = c.Status,
        CharacteristicCount = c.Characteristics.Count
    }));
})
.WithName("GetClasses")
.WithTags("Classes")
.Produces<List<ClassDto>>(200);

app.MapGet("/api/classes/{className}", async (
    string className,
    SqliteDbContext context) =>
{
    var classEntity = await context.Classes
        .Include(c => c.Characteristics)
        .ThenInclude(ch => ch.Values)
        .FirstOrDefaultAsync(c => c.Name == className);

    if (classEntity == null)
        return Results.NotFound(new { error = $"Class '{className}' not found" });

    return Results.Ok(new ClassDetailDto
    {
        Name = classEntity.Name,
        ClassType = classEntity.ClassType,
        Description = classEntity.Description,
        Status = classEntity.Status,
        Characteristics = classEntity.Characteristics
            .OrderBy(ch => ch.SortOrder)
            .ThenBy(ch => ch.Name)
            .Select(ch => new CharacteristicDto
            {
                Name = ch.Name,
                Description = ch.Description,
                LongDescription = ch.LongDescription,
                DataType = ch.DataType,
                Length = ch.Length,
                IsRequired = ch.IsRequired,
                IsRestrictable = ch.IsRestrictable,
                IsHidden = ch.IsHidden,
                SortOrder = ch.SortOrder,
                AllowedValues = ch.Values.Select(v => new CharacteristicValueDto
                {
                    Value = v.Value,
                    Description = v.Description,
                    LongDescription = v.LongDescription,
                    IsDefault = v.IsDefault
                }).ToList()
            }).ToList()
    });
})
.WithName("GetClass")
.WithTags("Classes")
.Produces<ClassDetailDto>(200)
.Produces(404);

app.MapPost("/api/classes", async (
    CreateClassRequest request,
    SqliteDbContext context) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Class name is required" });

        if (string.IsNullOrWhiteSpace(request.ClassType))
            return Results.BadRequest(new { error = "Class type is required" });

        // Enforce uppercase for class name
        var className = request.Name.ToUpperInvariant();

        var existing = await context.Classes
            .FirstOrDefaultAsync(c => c.Name == className);

        if (existing != null)
            return Results.Conflict(new { error = $"Class '{className}' already exists" });

        var classEntity = new SapLoVcSharp.Data.Models.ClassEntity
        {
            Name = className,
            ClassType = request.ClassType,
            Description = request.Description ?? string.Empty,
            Status = request.Status ?? "Active"
        };

        context.Classes.Add(classEntity);
        await context.SaveChangesAsync();

        return Results.Created($"/api/classes/{classEntity.Name}", new
        {
            message = $"Class '{classEntity.Name}' created successfully",
            className = classEntity.Name
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateClass")
.WithTags("Classes")
.Produces(201)
.Produces(400)
.Produces(409)
.Produces(500);

app.MapPut("/api/classes/{className}", async (
    string className,
    UpdateClassRequest request,
    SqliteDbContext context) =>
{
    var classEntity = await context.Classes
        .FirstOrDefaultAsync(c => c.Name == className);

    if (classEntity == null)
        return Results.NotFound(new { error = $"Class '{className}' not found" });

    if (!string.IsNullOrWhiteSpace(request.Description))
        classEntity.Description = request.Description;

    if (!string.IsNullOrWhiteSpace(request.Status))
        classEntity.Status = request.Status;

    if (!string.IsNullOrWhiteSpace(request.ClassType))
        classEntity.ClassType = request.ClassType;

    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Class '{className}' updated successfully" });
})
.WithName("UpdateClass")
.WithTags("Classes")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapDelete("/api/classes/{className}", async (
    string className,
    SqliteDbContext context) =>
{
    var classEntity = await context.Classes
        .FirstOrDefaultAsync(c => c.Name == className);

    if (classEntity == null)
        return Results.NotFound(new { error = $"Class '{className}' not found" });

    context.Classes.Remove(classEntity);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Class '{className}' deleted successfully" });
})
.WithName("DeleteClass")
.WithTags("Classes")
.Produces(200)
.Produces(404)
.Produces(500);

// ============================================================================
// CHARACTERISTIC ENDPOINTS
// ============================================================================

app.MapPost("/api/classes/{className}/characteristics", async (
    string className,
    CreateCharacteristicRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var classEntity = await context.Classes
            .Include(c => c.Characteristics)
            .FirstOrDefaultAsync(c => c.Name == className);

        if (classEntity == null)
            return Results.NotFound(new { error = $"Class '{className}' not found" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Characteristic name is required" });

        // Enforce uppercase for characteristic name
        var charName = request.Name.ToUpperInvariant();

        var existing = classEntity.Characteristics
            .FirstOrDefault(ch => ch.Name == charName);

        if (existing != null)
            return Results.Conflict(new { error = $"Characteristic '{charName}' already exists in class '{className}'" });

        var characteristic = new SapLoVcSharp.Data.Models.CharacteristicEntity
        {
            ClassName = className,
            Name = charName,
            Description = request.Description ?? string.Empty,
            LongDescription = request.LongDescription ?? string.Empty,
            DataType = request.DataType ?? "CHAR",
            Length = request.Length,
            IsRequired = request.IsRequired,
            IsRestrictable = request.IsRestrictable,
            IsHidden = request.IsHidden,
            SortOrder = request.SortOrder
        };

        // Add values if provided - enforce uppercase for values
        if (request.AllowedValues != null && request.AllowedValues.Any())
        {
            foreach (var value in request.AllowedValues)
            {
                characteristic.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
                {
                    ClassName = className,
                    CharacteristicName = charName,
                    Value = value.Value.ToUpperInvariant(),
                    Description = value.Description ?? string.Empty,
                    LongDescription = value.LongDescription ?? string.Empty,
                    IsDefault = value.IsDefault
                });
            }
        }

        classEntity.Characteristics.Add(characteristic);
        await context.SaveChangesAsync();

        return Results.Created($"/api/classes/{className}/characteristics/{charName}", new
        {
            message = $"Characteristic '{charName}' created successfully",
            characteristicName = charName
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateCharacteristic")
.WithTags("Characteristics")
.Produces(201)
.Produces(400)
.Produces(404)
.Produces(409)
.Produces(500);

app.MapPut("/api/classes/{className}/characteristics/{characteristicName}", async (
    string className,
    string characteristicName,
    UpdateCharacteristicRequest request,
    SqliteDbContext context) =>
{
    var characteristic = await context.Characteristics
        .Include(ch => ch.Values)
        .FirstOrDefaultAsync(ch => ch.ClassName == className && ch.Name == characteristicName);

    if (characteristic == null)
        return Results.NotFound(new { error = $"Characteristic '{characteristicName}' not found in class '{className}'" });

    if (!string.IsNullOrWhiteSpace(request.Description))
        characteristic.Description = request.Description;

    if (!string.IsNullOrWhiteSpace(request.LongDescription))
        characteristic.LongDescription = request.LongDescription;

    if (!string.IsNullOrWhiteSpace(request.DataType))
        characteristic.DataType = request.DataType;

    if (request.Length.HasValue)
        characteristic.Length = request.Length.Value;

    if (request.IsRequired.HasValue)
        characteristic.IsRequired = request.IsRequired.Value;

    if (request.IsRestrictable.HasValue)
        characteristic.IsRestrictable = request.IsRestrictable.Value;

    if (request.IsHidden.HasValue)
        characteristic.IsHidden = request.IsHidden.Value;

    if (request.SortOrder.HasValue)
        characteristic.SortOrder = request.SortOrder.Value;

    // Update values if provided (even if empty array - to allow clearing all values)
    if (request.AllowedValues != null)
    {
        // Remove existing values
        context.CharacteristicValues.RemoveRange(characteristic.Values);

        // Add new values - enforce uppercase for values
        foreach (var value in request.AllowedValues)
        {
            characteristic.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = className,
                CharacteristicName = characteristicName,
                Value = value.Value.ToUpperInvariant(),
                Description = value.Description ?? string.Empty,
                LongDescription = value.LongDescription ?? string.Empty,
                IsDefault = value.IsDefault
            });
        }
    }

    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Characteristic '{characteristicName}' updated successfully" });
})
.WithName("UpdateCharacteristic")
.WithTags("Characteristics")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapDelete("/api/classes/{className}/characteristics/{characteristicName}", async (
    string className,
    string characteristicName,
    SqliteDbContext context) =>
{
    var characteristic = await context.Characteristics
        .FirstOrDefaultAsync(ch => ch.ClassName == className && ch.Name == characteristicName);

    if (characteristic == null)
        return Results.NotFound(new { error = $"Characteristic '{characteristicName}' not found in class '{className}'" });

    context.Characteristics.Remove(characteristic);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Characteristic '{characteristicName}' deleted successfully" });
})
.WithName("DeleteCharacteristic")
.WithTags("Characteristics")
.Produces(200)
.Produces(404)
.Produces(500);

// Reorder characteristics within a class
app.MapPut("/api/classes/{className}/characteristics/reorder", async (
    string className,
    ReorderCharacteristicsRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var classEntity = await context.Classes
            .Include(c => c.Characteristics)
            .FirstOrDefaultAsync(c => c.Name == className);

        if (classEntity == null)
            return Results.NotFound(new { error = $"Class '{className}' not found" });

        foreach (var item in request.Order)
        {
            var characteristic = classEntity.Characteristics
                .FirstOrDefault(ch => ch.Name == item.Name);

            if (characteristic != null)
            {
                characteristic.SortOrder = item.SortOrder;
            }
        }

        await context.SaveChangesAsync();

        return Results.Ok(new { message = $"Characteristics reordered successfully in class '{className}'" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("ReorderCharacteristics")
.WithTags("Characteristics")
.Produces(200)
.Produces(404)
.Produces(500);

// ============================================================================
// CONFIGURATION PROFILE ENDPOINTS
// ============================================================================

app.MapPost("/api/configuration-profiles", async (
    CreateConfigurationProfileRequest request,
    SqliteDbContext context) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.MaterialNumber))
            return Results.BadRequest(new { error = "Material number is required" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Profile name is required" });

        // Check if material exists
        var material = await context.Materials
            .FirstOrDefaultAsync(m => m.MaterialNumber == request.MaterialNumber);

        if (material == null)
            return Results.NotFound(new { error = $"Material '{request.MaterialNumber}' not found" });

        // Check if material already has a profile
        var existingProfile = await context.ConfigurationProfiles
            .FirstOrDefaultAsync(cp => cp.MaterialNumber == request.MaterialNumber);

        if (existingProfile != null)
            return Results.Conflict(new { error = $"Material '{request.MaterialNumber}' already has a configuration profile" });

        var profile = new SapLoVcSharp.Data.Models.ConfigurationProfileEntity
        {
            Id = Guid.NewGuid().ToString(),
            MaterialNumber = request.MaterialNumber,
            Name = request.Name
        };

        context.ConfigurationProfiles.Add(profile);
        await context.SaveChangesAsync();

        return Results.Created($"/api/configuration-profiles/{profile.Id}", new ConfigurationProfileDto
        {
            Id = profile.Id,
            MaterialNumber = profile.MaterialNumber,
            Name = profile.Name,
            Description = request.Description ?? string.Empty,
            Procedures = new List<ProcedureDto>()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateConfigurationProfile")
.WithTags("ConfigurationProfiles")
.Produces<ConfigurationProfileDto>(201)
.Produces(400)
.Produces(404)
.Produces(409)
.Produces(500);

app.MapGet("/api/configuration-profiles/material/{materialNumber}", async (
    string materialNumber,
    SqliteDbContext context) =>
{
    var profile = await context.ConfigurationProfiles
        .Include(cp => cp.Procedures)
        .ThenInclude(p => p.Dependency)
        .FirstOrDefaultAsync(cp => cp.MaterialNumber == materialNumber);

    if (profile == null)
        return Results.NotFound(new { error = $"No configuration profile found for material '{materialNumber}'" });

    return Results.Ok(new ConfigurationProfileDto
    {
        Id = profile.Id,
        MaterialNumber = profile.MaterialNumber,
        Name = profile.Name,
        Description = string.Empty,
        Procedures = profile.Procedures
            .OrderBy(p => p.SequenceNumber)
            .Select(p => new ProcedureDto
            {
                Id = p.Dependency!.Id,
                Name = p.Dependency.Name,
                SourceCode = p.Dependency.SourceCode,
                Status = p.Dependency.Status,
                Description = null
            }).ToList()
    });
})
.WithName("GetConfigurationProfileByMaterial")
.WithTags("ConfigurationProfiles")
.Produces<ConfigurationProfileDto>(200)
.Produces(404);

// ============================================================================
// PROCEDURE ENDPOINTS
// ============================================================================

app.MapPost("/api/configuration-profiles/{profileId}/procedures", async (
    string profileId,
    CreateProcedureRequest request,
    SqliteDbContext context) =>
{
    try
    {
        // Validate profile exists
        var profile = await context.ConfigurationProfiles
            .Include(cp => cp.Procedures)
            .FirstOrDefaultAsync(cp => cp.Id == profileId);

        if (profile == null)
            return Results.NotFound(new { error = $"Configuration profile '{profileId}' not found" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Procedure name is required" });

        if (string.IsNullOrWhiteSpace(request.SourceCode))
            return Results.BadRequest(new { error = "Source code is required" });

        // Parse source code to AST
        string astJson;
        try
        {
            var lexer = new Lexer(request.SourceCode);
            var parser = new Parser(lexer);
            var ast = parser.ParseDependency(request.SourceCode, DependencyType.Procedure);
            var serializer = new AstSerializer();
            astJson = serializer.Serialize(ast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Failed to parse source code: {ex.Message}" });
        }

        // Enforce uppercase for procedure name
        var procedureName = request.Name.ToUpperInvariant();

        // Create dependency entity for procedure
        var dependency = new SapLoVcSharp.Data.Models.DependencyEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = procedureName,
            Type = "Procedure",
            SourceCode = request.SourceCode,
            AstJson = astJson,
            Status = "Active"
        };

        context.Dependencies.Add(dependency);

        // Link procedure to profile
        var nextSequence = profile.Procedures.Any()
            ? profile.Procedures.Max(p => p.SequenceNumber) + 1
            : 0;

        var profileProcedure = new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
        {
            ConfigurationProfileId = profileId,
            DependencyId = dependency.Id,
            SequenceNumber = nextSequence
        };

        context.ConfigurationProfileProcedures.Add(profileProcedure);
        await context.SaveChangesAsync();

        return Results.Created($"/api/procedures/{dependency.Id}", new ProcedureDto
        {
            Id = dependency.Id,
            Name = dependency.Name,
            SourceCode = dependency.SourceCode,
            Status = dependency.Status,
            Description = request.Description
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateProcedure")
.WithTags("Procedures")
.Produces<ProcedureDto>(201)
.Produces(400)
.Produces(404)
.Produces(500);

app.MapPut("/api/procedures/{procedureId}", async (
    string procedureId,
    CreateProcedureRequest request,
    SqliteDbContext context) =>
{
    var dependency = await context.Dependencies
        .FirstOrDefaultAsync(d => d.Id == procedureId && d.Type == "Procedure");

    if (dependency == null)
        return Results.NotFound(new { error = $"Procedure '{procedureId}' not found" });

    if (!string.IsNullOrWhiteSpace(request.Name))
        dependency.Name = request.Name;

    if (!string.IsNullOrWhiteSpace(request.Status))
        dependency.Status = request.Status;

    if (!string.IsNullOrWhiteSpace(request.SourceCode))
    {
        // Parse source code to AST
        try
        {
            var lexer = new Lexer(request.SourceCode);
            var parser = new Parser(lexer);
            var ast = parser.ParseDependency(request.SourceCode, DependencyType.Procedure);
            var serializer = new AstSerializer();
            dependency.AstJson = serializer.Serialize(ast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Failed to parse source code: {ex.Message}" });
        }

        dependency.SourceCode = request.SourceCode;
        dependency.Version++;
    }

    dependency.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Procedure '{procedureId}' updated successfully" });
})
.WithName("UpdateProcedure")
.WithTags("Procedures")
.Produces(200)
.Produces(404)
.Produces(500);

app.MapDelete("/api/procedures/{procedureId}", async (
    string procedureId,
    SqliteDbContext context) =>
{
    var dependency = await context.Dependencies
        .FirstOrDefaultAsync(d => d.Id == procedureId && d.Type == "Procedure");

    if (dependency == null)
        return Results.NotFound(new { error = $"Procedure '{procedureId}' not found" });

    // First remove all procedure assignments (ConfigurationProfileProcedureEntity)
    var procedureAssignments = await context.ConfigurationProfileProcedures
        .Where(p => p.DependencyId == procedureId)
        .ToListAsync();

    context.ConfigurationProfileProcedures.RemoveRange(procedureAssignments);

    // Then delete the dependency itself
    context.Dependencies.Remove(dependency);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Procedure '{procedureId}' deleted successfully" });
})
.WithName("DeleteProcedure")
.WithTags("Procedures")
.Produces(200)
.Produces(404)
.Produces(500);

// Debug endpoint to view procedure AST
app.MapGet("/api/procedures/{procedureId}/debug", async (
    string procedureId,
    SqliteDbContext context) =>
{
    var dependency = await context.Dependencies
        .FirstOrDefaultAsync(d => d.Id == procedureId && d.Type == "Procedure");

    if (dependency == null)
        return Results.NotFound(new { error = $"Procedure '{procedureId}' not found" });

    return Results.Ok(new
    {
        id = dependency.Id,
        name = dependency.Name,
        sourceCode = dependency.SourceCode,
        astJson = dependency.AstJson,
        status = dependency.Status,
        version = dependency.Version
    });
})
.WithName("DebugProcedure")
.WithTags("Procedures")
.Produces(200)
.Produces(404);

// ============================================================================
// CONSTRAINT NET ENDPOINTS
// ============================================================================

app.MapGet("/api/constraint-nets/material/{materialNumber}", async (
    string materialNumber,
    SqliteDbContext context) =>
{
    var profile = await context.ConfigurationProfiles
        .Include(cp => cp.DependencyNets)
        .ThenInclude(dn => dn.Constraints)
        .ThenInclude(c => c.Dependency)
        .FirstOrDefaultAsync(cp => cp.MaterialNumber == materialNumber);

    if (profile == null)
        return Results.Ok(new List<ConstraintNetDto>());

    var nets = profile.DependencyNets.Select(net => new ConstraintNetDto
    {
        Id = net.Id,
        Name = net.Name,
        Description = net.Description,
        Dependencies = net.Constraints
            .OrderBy(c => c.SequenceNumber)
            .Select(c => new DependencyDto
            {
                Id = c.Dependency!.Id,
                Name = c.Dependency.Name,
                Type = c.Dependency.Type,
                SourceCode = c.Dependency.SourceCode,
                Status = c.Dependency.Status
            }).ToList()
    }).ToList();

    return Results.Ok(nets);
})
.WithName("GetConstraintNetsByMaterial")
.WithTags("ConstraintNets")
.Produces<List<ConstraintNetDto>>(200);

app.MapPost("/api/constraint-nets/material/{materialNumber}", async (
    string materialNumber,
    CreateConstraintNetRequest request,
    SqliteDbContext context) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Constraint net name is required" });

        // Get or create configuration profile
        var profile = await context.ConfigurationProfiles
            .FirstOrDefaultAsync(cp => cp.MaterialNumber == materialNumber);

        if (profile == null)
            return Results.NotFound(new { error = $"No configuration profile found for material '{materialNumber}'" });

        // Enforce uppercase for constraint net name
        var netName = request.Name.ToUpperInvariant();

        var net = new SapLoVcSharp.Data.Models.DependencyNetEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = netName,
            Description = request.Description ?? string.Empty,
            ConfigurationProfileId = profile.Id
        };

        context.DependencyNets.Add(net);
        await context.SaveChangesAsync();

        return Results.Created($"/api/constraint-nets/{net.Id}", new ConstraintNetDto
        {
            Id = net.Id,
            Name = net.Name,
            Description = net.Description,
            Dependencies = new List<DependencyDto>()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateConstraintNet")
.WithTags("ConstraintNets")
.Produces<ConstraintNetDto>(201)
.Produces(400)
.Produces(404)
.Produces(500);

app.MapDelete("/api/constraint-nets/{netId}", async (
    string netId,
    SqliteDbContext context) =>
{
    var net = await context.DependencyNets
        .FirstOrDefaultAsync(n => n.Id == netId);

    if (net == null)
        return Results.NotFound(new { error = $"Constraint net '{netId}' not found" });

    context.DependencyNets.Remove(net);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Constraint net '{netId}' deleted successfully" });
})
.WithName("DeleteConstraintNet")
.WithTags("ConstraintNets")
.Produces(200)
.Produces(404)
.Produces(500);

// ============================================================================
// DEPENDENCY ENDPOINTS (Constraints assigned to nets)
// ============================================================================

app.MapPost("/api/constraint-nets/{netId}/dependencies", async (
    string netId,
    CreateDependencyRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var net = await context.DependencyNets
            .Include(n => n.Constraints)
            .FirstOrDefaultAsync(n => n.Id == netId);

        if (net == null)
            return Results.NotFound(new { error = $"Constraint net '{netId}' not found" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Dependency name is required" });

        if (string.IsNullOrWhiteSpace(request.SourceCode))
            return Results.BadRequest(new { error = "Source code is required" });

        // Map string type to DependencyType enum
        DependencyType dependencyType;
        try
        {
            dependencyType = Enum.Parse<DependencyType>(request.Type, ignoreCase: true);
        }
        catch
        {
            return Results.BadRequest(new { error = $"Invalid dependency type: {request.Type}" });
        }

        // Parse source code to AST
        string astJson;
        try
        {
            var lexer = new Lexer(request.SourceCode);
            var parser = new Parser(lexer);
            var ast = parser.ParseDependency(request.SourceCode, dependencyType);
            var serializer = new AstSerializer();
            astJson = serializer.Serialize(ast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Failed to parse source code: {ex.Message}" });
        }

        // Enforce uppercase for dependency name
        var dependencyName = request.Name.ToUpperInvariant();

        // Create dependency entity
        var dependency = new SapLoVcSharp.Data.Models.DependencyEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = dependencyName,
            Type = request.Type,
            SourceCode = request.SourceCode,
            AstJson = astJson,
            Status = "Active"
        };

        context.Dependencies.Add(dependency);

        // Link to constraint net
        var nextSequence = net.Constraints.Any()
            ? net.Constraints.Max(c => c.SequenceNumber) + 1
            : 0;

        var netConstraint = new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
        {
            DependencyNetId = netId,
            DependencyId = dependency.Id,
            SequenceNumber = nextSequence
        };

        context.DependencyNetConstraints.Add(netConstraint);
        await context.SaveChangesAsync();

        return Results.Created($"/api/dependencies/{dependency.Id}", new DependencyDto
        {
            Id = dependency.Id,
            Name = dependency.Name,
            Type = dependency.Type,
            SourceCode = dependency.SourceCode,
            Status = dependency.Status
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateDependency")
.WithTags("Dependencies")
.Produces<DependencyDto>(201)
.Produces(400)
.Produces(404)
.Produces(500);

// Update Dependency
app.MapPut("/api/dependencies/{dependencyId}", async (
    string dependencyId,
    CreateDependencyRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var dependency = await context.Dependencies
            .FirstOrDefaultAsync(d => d.Id == dependencyId);

        if (dependency == null)
            return Results.NotFound(new { error = $"Dependency '{dependencyId}' not found" });

        // Parse dependency type
        if (!Enum.TryParse<DependencyType>(request.Type, true, out var dependencyType))
        {
            return Results.BadRequest(new { error = $"Invalid dependency type: {request.Type}" });
        }

        // Parse source code to AST
        string astJson;
        try
        {
            var lexer = new Lexer(request.SourceCode);
            var parser = new Parser(lexer);
            var ast = parser.ParseDependency(request.SourceCode, dependencyType);
            var serializer = new AstSerializer();
            astJson = serializer.Serialize(ast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Failed to parse source code: {ex.Message}" });
        }

        // Update dependency entity
        dependency.Name = request.Name;
        dependency.Type = request.Type;
        dependency.SourceCode = request.SourceCode;
        dependency.AstJson = astJson;
        if (!string.IsNullOrWhiteSpace(request.Status))
            dependency.Status = request.Status;
        dependency.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Results.Ok(new DependencyDto
        {
            Id = dependency.Id,
            Name = dependency.Name,
            Type = dependency.Type,
            SourceCode = dependency.SourceCode,
            Status = dependency.Status
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UpdateDependency")
.WithTags("Dependencies")
.Produces<DependencyDto>(200)
.Produces(400)
.Produces(404)
.Produces(500);

app.MapDelete("/api/dependencies/{dependencyId}", async (
    string dependencyId,
    SqliteDbContext context) =>
{
    var dependency = await context.Dependencies
        .FirstOrDefaultAsync(d => d.Id == dependencyId);

    if (dependency == null)
        return Results.NotFound(new { error = $"Dependency '{dependencyId}' not found" });

    // First remove all constraint net assignments (DependencyNetConstraintEntity)
    var netAssignments = await context.DependencyNetConstraints
        .Where(dnc => dnc.DependencyId == dependencyId)
        .ToListAsync();

    context.DependencyNetConstraints.RemoveRange(netAssignments);

    // Then delete the dependency itself
    context.Dependencies.Remove(dependency);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Dependency '{dependencyId}' deleted successfully" });
})
.WithName("DeleteDependency")
.WithTags("Dependencies")
.Produces(200)
.Produces(404)
.Produces(500);

// ============================================================================
// PRODUCT MATRIX ENDPOINTS
// ============================================================================

// List all product matrices for a material
app.MapGet("/api/materials/{materialNumber}/product-matrices", async (
    string materialNumber,
    SqliteDbContext context) =>
{
    var matrices = await context.ProductMatrices
        .Include(m => m.Sheets)
        .Where(m => m.MaterialNumber == materialNumber)
        .ToListAsync();

    return Results.Ok(matrices.Select(m => new ProductMatrixListDto
    {
        Name = m.Name,
        Description = m.Description,
        Status = m.Status,
        MaterialNumber = m.MaterialNumber,
        GeneratedVariantTableName = m.GeneratedVariantTableName,
        SheetCount = m.Sheets.Count
    }));
})
.WithName("GetProductMatricesByMaterial")
.WithTags("ProductMatrices")
.Produces<List<ProductMatrixListDto>>(200);

// Get a specific product matrix with all details
app.MapGet("/api/product-matrices/{matrixName}", async (
    string matrixName,
    SqliteDbContext context) =>
{
    var matrix = await context.ProductMatrices
        .Include(m => m.Sheets)
        .ThenInclude(s => s.Characteristics)
        .Include(m => m.Sheets)
        .ThenInclude(s => s.Rows)
        .ThenInclude(r => r.Cells)
        .FirstOrDefaultAsync(m => m.Name == matrixName);

    if (matrix == null)
        return Results.NotFound(new { error = $"Product matrix '{matrixName}' not found" });

    // Helper to fetch characteristic allowed values
    async Task<List<string>> GetAllowedValues(string className, string charName)
    {
        var characteristic = await context.Characteristics
            .Include(c => c.Values)
            .FirstOrDefaultAsync(c => c.ClassName == className && c.Name == charName);
        return characteristic?.Values.Select(v => v.Value).ToList() ?? new List<string>();
    }

    var sheetsDto = new List<ProductMatrixSheetDto>();

    foreach (var sheet in matrix.Sheets.OrderBy(s => s.SheetIndex))
    {
        var characteristicsDto = new List<ProductMatrixCharacteristicDto>();

        foreach (var ch in sheet.Characteristics.OrderBy(c => c.CharacteristicIndex))
        {
            var allowedValues = await GetAllowedValues(ch.ClassName, ch.CharacteristicName);
            List<string>? resultAllowedValues = null;

            if (!string.IsNullOrEmpty(ch.ResultCharacteristicName) && !string.IsNullOrEmpty(ch.ResultClassName))
            {
                resultAllowedValues = await GetAllowedValues(ch.ResultClassName, ch.ResultCharacteristicName);
            }

            characteristicsDto.Add(new ProductMatrixCharacteristicDto
            {
                CharacteristicIndex = ch.CharacteristicIndex,
                CharacteristicName = ch.CharacteristicName,
                ClassName = ch.ClassName,
                DisplayMode = ch.DisplayMode,
                ResultCharacteristicName = ch.ResultCharacteristicName,
                ResultClassName = ch.ResultClassName,
                AllowedValues = allowedValues,
                ResultAllowedValues = resultAllowedValues
            });
        }

        sheetsDto.Add(new ProductMatrixSheetDto
        {
            SheetIndex = sheet.SheetIndex,
            DisplayName = sheet.DisplayName,
            Characteristics = characteristicsDto,
            Rows = sheet.Rows.OrderBy(r => r.RowIndex).Select(r => new ProductMatrixRowDto
            {
                RowIndex = r.RowIndex,
                RowLabel = r.RowLabel,
                Cells = r.Cells.OrderBy(c => c.CharacteristicIndex).ThenBy(c => c.ValueIndex).Select(c => new ProductMatrixCellDto
                {
                    CharacteristicIndex = c.CharacteristicIndex,
                    ValueIndex = c.ValueIndex,
                    Value = c.Value
                }).ToList()
            }).ToList()
        });
    }

    return Results.Ok(new ProductMatrixDto
    {
        Name = matrix.Name,
        Description = matrix.Description,
        Status = matrix.Status,
        MaterialNumber = matrix.MaterialNumber,
        GeneratedVariantTableName = matrix.GeneratedVariantTableName,
        Sheets = sheetsDto
    });
})
.WithName("GetProductMatrix")
.WithTags("ProductMatrices")
.Produces<ProductMatrixDto>(200)
.Produces(404);

// Create a new product matrix
app.MapPost("/api/product-matrices", async (
    CreateProductMatrixRequest request,
    SqliteDbContext context) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Matrix name is required" });

        if (string.IsNullOrWhiteSpace(request.MaterialNumber))
            return Results.BadRequest(new { error = "Material number is required" });

        // Enforce uppercase for matrix name
        var matrixName = request.Name.ToUpperInvariant();

        // Check if material exists
        var material = await context.Materials
            .FirstOrDefaultAsync(m => m.MaterialNumber == request.MaterialNumber);

        if (material == null)
            return Results.NotFound(new { error = $"Material '{request.MaterialNumber}' not found" });

        // Check if matrix already exists
        var existing = await context.ProductMatrices
            .FirstOrDefaultAsync(m => m.Name == matrixName);

        if (existing != null)
            return Results.Conflict(new { error = $"Product matrix '{matrixName}' already exists" });

        var matrix = new SapLoVcSharp.Data.Models.ProductMatrixEntity
        {
            Name = matrixName,
            Description = request.Description ?? string.Empty,
            Status = request.Status ?? "Active",
            MaterialNumber = request.MaterialNumber
        };

        context.ProductMatrices.Add(matrix);
        await context.SaveChangesAsync();

        return Results.Created($"/api/product-matrices/{matrixName}", new ProductMatrixListDto
        {
            Name = matrix.Name,
            Description = matrix.Description,
            Status = matrix.Status,
            MaterialNumber = matrix.MaterialNumber,
            GeneratedVariantTableName = null,
            SheetCount = 0
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("CreateProductMatrix")
.WithTags("ProductMatrices")
.Produces<ProductMatrixListDto>(201)
.Produces(400)
.Produces(404)
.Produces(409)
.Produces(500);

// Update product matrix metadata
app.MapPut("/api/product-matrices/{matrixName}", async (
    string matrixName,
    UpdateProductMatrixRequest request,
    SqliteDbContext context) =>
{
    var matrix = await context.ProductMatrices
        .FirstOrDefaultAsync(m => m.Name == matrixName);

    if (matrix == null)
        return Results.NotFound(new { error = $"Product matrix '{matrixName}' not found" });

    if (!string.IsNullOrWhiteSpace(request.Description))
        matrix.Description = request.Description;

    if (!string.IsNullOrWhiteSpace(request.Status))
        matrix.Status = request.Status;

    matrix.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Product matrix '{matrixName}' updated successfully" });
})
.WithName("UpdateProductMatrix")
.WithTags("ProductMatrices")
.Produces(200)
.Produces(404)
.Produces(500);

// Delete a product matrix
app.MapDelete("/api/product-matrices/{matrixName}", async (
    string matrixName,
    SqliteDbContext context) =>
{
    var matrix = await context.ProductMatrices
        .FirstOrDefaultAsync(m => m.Name == matrixName);

    if (matrix == null)
        return Results.NotFound(new { error = $"Product matrix '{matrixName}' not found" });

    context.ProductMatrices.Remove(matrix);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Product matrix '{matrixName}' deleted successfully" });
})
.WithName("DeleteProductMatrix")
.WithTags("ProductMatrices")
.Produces(200)
.Produces(404)
.Produces(500);

// Add a sheet to a product matrix
app.MapPost("/api/product-matrices/{matrixName}/sheets", async (
    string matrixName,
    CreateProductMatrixSheetRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var matrix = await context.ProductMatrices
            .Include(m => m.Sheets)
            .FirstOrDefaultAsync(m => m.Name == matrixName);

        if (matrix == null)
            return Results.NotFound(new { error = $"Product matrix '{matrixName}' not found" });

        var nextSheetIndex = matrix.Sheets.Any() ? matrix.Sheets.Max(s => s.SheetIndex) + 1 : 0;

        var sheet = new SapLoVcSharp.Data.Models.ProductMatrixSheetEntity
        {
            MatrixName = matrixName,
            SheetIndex = nextSheetIndex,
            DisplayName = request.DisplayName
        };

        context.ProductMatrixSheets.Add(sheet);
        await context.SaveChangesAsync();

        return Results.Created($"/api/product-matrices/{matrixName}/sheets/{nextSheetIndex}",
            new { message = $"Sheet added to matrix '{matrixName}'", sheetIndex = nextSheetIndex });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("AddProductMatrixSheet")
.WithTags("ProductMatrices")
.Produces(201)
.Produces(400)
.Produces(404)
.Produces(500);

// Update a sheet (characteristics, rows, cells)
app.MapPut("/api/product-matrices/{matrixName}/sheets/{sheetIndex:int}", async (
    string matrixName,
    int sheetIndex,
    UpdateProductMatrixSheetRequest request,
    SqliteDbContext context) =>
{
    try
    {
        var sheet = await context.ProductMatrixSheets
            .Include(s => s.Characteristics)
            .Include(s => s.Rows)
            .ThenInclude(r => r.Cells)
            .FirstOrDefaultAsync(s => s.MatrixName == matrixName && s.SheetIndex == sheetIndex);

        if (sheet == null)
            return Results.NotFound(new { error = $"Sheet {sheetIndex} not found in matrix '{matrixName}'" });

        // Update display name if provided
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            sheet.DisplayName = request.DisplayName;

        // Update characteristics if provided
        if (request.Characteristics != null)
        {
            // Remove existing characteristics
            context.ProductMatrixCharacteristics.RemoveRange(sheet.Characteristics);

            // Add new characteristics
            foreach (var ch in request.Characteristics)
            {
                context.ProductMatrixCharacteristics.Add(new SapLoVcSharp.Data.Models.ProductMatrixCharacteristicEntity
                {
                    MatrixName = matrixName,
                    SheetIndex = sheetIndex,
                    CharacteristicIndex = ch.CharacteristicIndex,
                    CharacteristicName = ch.CharacteristicName.ToUpperInvariant(),
                    ClassName = ch.ClassName.ToUpperInvariant(),
                    DisplayMode = ch.DisplayMode,
                    ResultCharacteristicName = ch.ResultCharacteristicName?.ToUpperInvariant(),
                    ResultClassName = ch.ResultClassName?.ToUpperInvariant()
                });
            }
        }

        // Update rows if provided
        if (request.Rows != null)
        {
            // Remove existing rows (cells will cascade)
            context.ProductMatrixRows.RemoveRange(sheet.Rows);

            // Add new rows
            foreach (var row in request.Rows)
            {
                var newRow = new SapLoVcSharp.Data.Models.ProductMatrixRowEntity
                {
                    MatrixName = matrixName,
                    SheetIndex = sheetIndex,
                    RowIndex = row.RowIndex,
                    RowLabel = row.RowLabel?.ToUpperInvariant()
                };

                context.ProductMatrixRows.Add(newRow);

                // Add cells
                foreach (var cell in row.Cells)
                {
                    context.ProductMatrixCells.Add(new SapLoVcSharp.Data.Models.ProductMatrixCellEntity
                    {
                        MatrixName = matrixName,
                        SheetIndex = sheetIndex,
                        RowIndex = row.RowIndex,
                        CharacteristicIndex = cell.CharacteristicIndex,
                        ValueIndex = cell.ValueIndex,
                        Value = cell.Value?.ToUpperInvariant()
                    });
                }
            }
        }

        // Update matrix timestamp
        var matrix = await context.ProductMatrices.FindAsync(matrixName);
        if (matrix != null)
            matrix.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Results.Ok(new { message = $"Sheet {sheetIndex} updated in matrix '{matrixName}'" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("UpdateProductMatrixSheet")
.WithTags("ProductMatrices")
.Produces(200)
.Produces(404)
.Produces(500);

// Delete a sheet
app.MapDelete("/api/product-matrices/{matrixName}/sheets/{sheetIndex:int}", async (
    string matrixName,
    int sheetIndex,
    SqliteDbContext context) =>
{
    var sheet = await context.ProductMatrixSheets
        .FirstOrDefaultAsync(s => s.MatrixName == matrixName && s.SheetIndex == sheetIndex);

    if (sheet == null)
        return Results.NotFound(new { error = $"Sheet {sheetIndex} not found in matrix '{matrixName}'" });

    context.ProductMatrixSheets.Remove(sheet);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = $"Sheet {sheetIndex} deleted from matrix '{matrixName}'" });
})
.WithName("DeleteProductMatrixSheet")
.WithTags("ProductMatrices")
.Produces(200)
.Produces(404)
.Produces(500);

// Generate variant table from product matrix
app.MapPost("/api/product-matrices/{matrixName}/generate", async (
    string matrixName,
    GenerateVariantTableFromMatrixRequest? request,
    SqliteDbContext context,
    VariantTableManager tableManager) =>
{
    try
    {
        var matrix = await context.ProductMatrices
            .Include(m => m.Sheets)
            .ThenInclude(s => s.Characteristics)
            .Include(m => m.Sheets)
            .ThenInclude(s => s.Rows)
            .ThenInclude(r => r.Cells)
            .FirstOrDefaultAsync(m => m.Name == matrixName);

        if (matrix == null)
            return Results.NotFound(new { error = $"Product matrix '{matrixName}' not found" });

        if (!matrix.Sheets.Any())
            return Results.BadRequest(new { error = "Matrix has no sheets" });

        // Determine variant table name
        var vtName = request?.VariantTableName ?? $"VT_{matrixName}";
        var vtDescription = request?.Description ?? $"Generated from Product Matrix {matrixName}";

        // Collect all unique characteristics needed for the variant table columns
        // Ordered so that result characteristics appear immediately after their main characteristic
        var allCharacteristics = new Dictionary<string, (string ClassName, bool IsKeyColumn)>();
        var orderedColumnNames = new List<string>();  // Maintains order: main char, then its result char

        foreach (var sheet in matrix.Sheets.OrderBy(s => s.SheetIndex))
        {
            foreach (var charDef in sheet.Characteristics.OrderBy(c => c.CharacteristicIndex))
            {
                // Add main characteristic
                if (!allCharacteristics.ContainsKey(charDef.CharacteristicName))
                {
                    allCharacteristics[charDef.CharacteristicName] = (charDef.ClassName, true);
                    orderedColumnNames.Add(charDef.CharacteristicName);
                }

                // Add result characteristic immediately after main characteristic
                if (!string.IsNullOrEmpty(charDef.ResultCharacteristicName) &&
                    !allCharacteristics.ContainsKey(charDef.ResultCharacteristicName))
                {
                    allCharacteristics[charDef.ResultCharacteristicName] = (charDef.ResultClassName!, false);
                    orderedColumnNames.Add(charDef.ResultCharacteristicName);
                }
            }
        }

        if (!allCharacteristics.Any())
            return Results.BadRequest(new { error = "Matrix has no characteristics defined" });

        // Helper to fetch allowed values for a characteristic
        async Task<List<string>> GetAllowedValues(string className, string charName)
        {
            var characteristic = await context.Characteristics
                .Include(c => c.Values)
                .FirstOrDefaultAsync(c => c.ClassName == className && c.Name == charName);
            return characteristic?.Values.Select(v => v.Value).ToList() ?? new List<string>();
        }

        // Build lookup for characteristic allowed values
        var charAllowedValues = new Dictionary<string, List<string>>();
        foreach (var sheet in matrix.Sheets)
        {
            foreach (var charDef in sheet.Characteristics.OrderBy(c => c.CharacteristicIndex))
            {
                if (!charAllowedValues.ContainsKey(charDef.CharacteristicName))
                {
                    charAllowedValues[charDef.CharacteristicName] = await GetAllowedValues(charDef.ClassName, charDef.CharacteristicName);
                }
                if (!string.IsNullOrEmpty(charDef.ResultCharacteristicName) &&
                    !charAllowedValues.ContainsKey(charDef.ResultCharacteristicName))
                {
                    charAllowedValues[charDef.ResultCharacteristicName] = await GetAllowedValues(charDef.ResultClassName!, charDef.ResultCharacteristicName);
                }
            }
        }

        // Generate variant table rows
        // Each matrix row can produce multiple VT rows (one per X-marked value in Standard mode)
        var vtRows = new List<Dictionary<string, string>>();

        foreach (var sheet in matrix.Sheets.OrderBy(s => s.SheetIndex))
        {
            var sheetChars = sheet.Characteristics.OrderBy(c => c.CharacteristicIndex).ToList();

            foreach (var row in sheet.Rows.OrderBy(r => r.RowIndex))
            {
                // For this matrix row, we need to generate VT rows
                // Start with a base row that has Column-style values filled in
                var baseRowValues = new Dictionary<string, string>();

                // First pass: collect Column-style values (these are fixed for all generated rows)
                foreach (var charDef in sheetChars.Where(c => c.DisplayMode == "Column"))
                {
                    var cell = row.Cells.FirstOrDefault(c => c.CharacteristicIndex == charDef.CharacteristicIndex && c.ValueIndex == 0);
                    if (cell?.Value != null)
                    {
                        baseRowValues[charDef.CharacteristicName] = cell.Value;
                    }
                }

                // Second pass: generate rows for Standard-style characteristics
                // Each combination of X-marked values generates a row
                var standardChars = sheetChars.Where(c => c.DisplayMode == "Standard").ToList();

                if (!standardChars.Any())
                {
                    // No standard chars, just add the base row if it has values
                    if (baseRowValues.Any())
                    {
                        vtRows.Add(new Dictionary<string, string>(baseRowValues));
                    }
                    continue;
                }

                // Build combinations from standard characteristics
                // Each standard char contributes: list of (charValue, resultValue?) tuples for X-marked or filled cells
                var charValueSets = new List<List<(string CharName, string CharValue, string? ResultCharName, string? ResultValue)>>();

                foreach (var charDef in standardChars)
                {
                    var allowedValues = charAllowedValues.GetValueOrDefault(charDef.CharacteristicName, new List<string>());
                    var valueSet = new List<(string CharName, string CharValue, string? ResultCharName, string? ResultValue)>();

                    for (int valueIdx = 0; valueIdx < allowedValues.Count; valueIdx++)
                    {
                        var cell = row.Cells.FirstOrDefault(c => c.CharacteristicIndex == charDef.CharacteristicIndex && c.ValueIndex == valueIdx);

                        if (charDef.ResultCharacteristicName != null)
                        {
                            // Standard with result: include if cell has a result value
                            if (!string.IsNullOrEmpty(cell?.Value))
                            {
                                valueSet.Add((charDef.CharacteristicName, allowedValues[valueIdx], charDef.ResultCharacteristicName, cell.Value));
                            }
                        }
                        else
                        {
                            // Standard without result: include if cell is "X"
                            if (cell?.Value == "X")
                            {
                                valueSet.Add((charDef.CharacteristicName, allowedValues[valueIdx], null, null));
                            }
                        }
                    }

                    if (valueSet.Any())
                    {
                        charValueSets.Add(valueSet);
                    }
                }

                // Generate cartesian product of all value sets
                if (charValueSets.Any())
                {
                    var combinations = CartesianProduct(charValueSets);

                    foreach (var combination in combinations)
                    {
                        var vtRow = new Dictionary<string, string>(baseRowValues);

                        foreach (var (charName, charValue, resultCharName, resultValue) in combination)
                        {
                            vtRow[charName] = charValue;
                            if (resultCharName != null && resultValue != null)
                            {
                                vtRow[resultCharName] = resultValue;
                            }
                        }

                        vtRows.Add(vtRow);
                    }
                }
                else if (baseRowValues.Any())
                {
                    // No standard values selected but have column values
                    vtRows.Add(new Dictionary<string, string>(baseRowValues));
                }
            }
        }

        if (!vtRows.Any())
            return Results.BadRequest(new { error = "No valid combinations found in the matrix. Make sure to mark cells with X or select values." });

        // Remove duplicate rows (use orderedColumnNames for consistent key generation)
        var uniqueRows = vtRows
            .Select(r => string.Join("|", orderedColumnNames.Select(k => r.GetValueOrDefault(k, ""))))
            .Distinct()
            .Select(key =>
            {
                var parts = key.Split('|');
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < orderedColumnNames.Count; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]))
                        dict[orderedColumnNames[i]] = parts[i];
                }
                return dict;
            })
            .Where(r => r.Any())
            .ToList();

        // Prepare data for VariantTableManager (use orderedColumnNames for proper column order)
        var columnNames = orderedColumnNames.ToArray();
        var sqlDataTypes = columnNames.Select(_ => "TEXT").ToArray();
        var isKeyColumns = columnNames.Select(cn => allCharacteristics[cn].IsKeyColumn).ToArray();

        // Convert rows to string arrays (in column order)
        var rowData = uniqueRows.Select(row =>
            columnNames.Select(cn => row.GetValueOrDefault(cn, "")).ToArray()
        ).ToArray();

        // Create the variant table
        await tableManager.CreateTableAsync(vtName, vtDescription, columnNames, sqlDataTypes, isKeyColumns, rowData);

        // Update matrix to reference generated table
        matrix.GeneratedVariantTableName = vtName;
        matrix.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Results.Ok(new
        {
            variantTableName = vtName,
            columnCount = columnNames.Length,
            rowCount = rowData.Length,
            columns = columnNames
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GenerateVariantTableFromMatrix")
.WithTags("ProductMatrices")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(500);

// Helper function for cartesian product
static IEnumerable<List<T>> CartesianProduct<T>(List<List<T>> sequences)
{
    if (!sequences.Any())
    {
        yield return new List<T>();
        yield break;
    }

    var first = sequences[0];
    var rest = sequences.Skip(1).ToList();

    foreach (var item in first)
    {
        foreach (var restCombination in CartesianProduct(rest))
        {
            var result = new List<T> { item };
            result.AddRange(restCombination);
            yield return result;
        }
    }
}

// ============================================================================
// HEALTH CHECK
// ============================================================================

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health")
.Produces(200);

app.Run();
