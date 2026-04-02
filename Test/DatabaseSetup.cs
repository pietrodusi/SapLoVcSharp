using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Data.Models;
using SapLoVcSharp.Data.Serialization;
using SapLoVcSharp.Data.Sqlite;

namespace TestConsole;

/// <summary>
/// Helper class to set up sample configuration data in the database
/// </summary>
public static class DatabaseSetup
{
    /// <summary>
    /// Creates a SQLite database with sample bike configuration data
    /// </summary>
    public static async Task<SqliteDbContext> CreateBikeDatabaseAsync(string databasePath = "bike-config.db")
    {
        // Delete existing database if it exists
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        // Create DbContext
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        var context = new SqliteDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Seed with sample data
        await SeedBikeDataAsync(context);

        return context;
    }

    private static async Task SeedBikeDataAsync(SqliteDbContext context)
    {
        var serializer = new AstSerializer();

        Console.WriteLine("Creating bike configuration data...");

        // 1. Create Class with Characteristics
        var bikeClass = new ClassEntity
        {
            Name = "BIKE_CLASS",
            ClassType = "300",
            Description = "Bicycle Configuration",
            Status = "Active"
        };

        var colorChar = new CharacteristicEntity
        {
            ClassName = "BIKE_CLASS",
            Name = "COLOR",
            Description = "Frame Color",
            DataType = "CHAR",
            IsRequired = true,
            Values = new List<CharacteristicValueEntity>
            {
                new() { Value = "Red", Description = "Red", IsDefault = true },
                new() { Value = "Blue", Description = "Blue" },
                new() { Value = "Green", Description = "Green" },
                new() { Value = "Black", Description = "Black" }
            }
        };

        var modelChar = new CharacteristicEntity
        {
            ClassName = "BIKE_CLASS",
            Name = "MODEL",
            Description = "Bike Model",
            DataType = "CHAR",
            IsRequired = true,
            Values = new List<CharacteristicValueEntity>
            {
                new() { Value = "Racing", Description = "Racing Bike" },
                new() { Value = "Mountain", Description = "Mountain Bike" },
                new() { Value = "City", Description = "City Bike", IsDefault = true }
            }
        };

        var frameHeightChar = new CharacteristicEntity
        {
            ClassName = "BIKE_CLASS",
            Name = "FRAME_HEIGHT",
            Description = "Frame Height in cm",
            DataType = "NUM",
            IsRequired = false
        };

        var gearCountChar = new CharacteristicEntity
        {
            ClassName = "BIKE_CLASS",
            Name = "GEAR_COUNT",
            Description = "Number of Gears",
            DataType = "NUM",
            IsRequired = false
        };

        var priceChar = new CharacteristicEntity
        {
            ClassName = "BIKE_CLASS",
            Name = "PRICE",
            Description = "Base Price",
            DataType = "NUM",
            IsRequired = false
        };

        bikeClass.Characteristics.Add(colorChar);
        bikeClass.Characteristics.Add(modelChar);
        bikeClass.Characteristics.Add(frameHeightChar);
        bikeClass.Characteristics.Add(gearCountChar);
        bikeClass.Characteristics.Add(priceChar);

        context.Classes.Add(bikeClass);
        Console.WriteLine($"  ✓ Created class: {bikeClass.Name} with {bikeClass.Characteristics.Count} characteristics");

        // 2. Create Material
        var bikeMaterial = new MaterialEntity
        {
            MaterialNumber = "BIKE-001",
            Description = "Configurable Bicycle",
            Status = "Active"
        };

        var classAssignment = new MaterialClassAssignmentEntity
        {
            MaterialNumber = "BIKE-001",
            ClassName = "BIKE_CLASS"
        };

        bikeMaterial.ClassAssignments.Add(classAssignment);
        context.Materials.Add(bikeMaterial);
        Console.WriteLine($"  ✓ Created material: {bikeMaterial.MaterialNumber}");

        // 3. Create Configuration Profile
        var profile = new ConfigurationProfileEntity
        {
            Id = "PROFILE-001",
            Name = "Bike Configuration Profile",
            MaterialNumber = "BIKE-001"
        };

        // 4. Create Procedures
        // Procedure 1: Set default frame height
        var frameHeightProc = new DependencyEntity
        {
            Id = "DEP-PROC-001",
            Name = "SetDefaultFrameHeight",
            Type = "Procedure",
            SourceCode = "$SELF.FRAME_HEIGHT = 54",
            Status = "Active",
            AstJson = serializer.Serialize(new ProcedureNode(
                new List<StatementNode>
                {
                    new AssignmentNode(
                        new MemberAccessNode(ObjectReference.Self, "FRAME_HEIGHT", new SourcePosition(0, 0, 0)),
                        new LiteralNode(54, LiteralType.Number, new SourcePosition(0, 0, 0)),
                        new SourcePosition(0, 0, 0)
                    )
                },
                new SourcePosition(0, 0, 0)
            ))
        };

        // Procedure 2: Calculate base price based on model
        var priceProc = new DependencyEntity
        {
            Id = "DEP-PROC-002",
            Name = "SetBasePrice",
            Type = "Procedure",
            SourceCode = "$SELF.PRICE = 1200",
            Status = "Active",
            AstJson = serializer.Serialize(new ProcedureNode(
                new List<StatementNode>
                {
                    new AssignmentNode(
                        new MemberAccessNode(ObjectReference.Self, "PRICE", new SourcePosition(0, 0, 0)),
                        new LiteralNode(1200, LiteralType.Number, new SourcePosition(0, 0, 0)),
                        new SourcePosition(0, 0, 0)
                    )
                },
                new SourcePosition(0, 0, 0)
            ))
        };

        context.Dependencies.Add(frameHeightProc);
        context.Dependencies.Add(priceProc);

        // Add procedures to profile
        profile.Procedures.Add(new ConfigurationProfileProcedureEntity
        {
            ConfigurationProfileId = profile.Id,
            DependencyId = frameHeightProc.Id,
            SequenceNumber = 1
        });

        profile.Procedures.Add(new ConfigurationProfileProcedureEntity
        {
            ConfigurationProfileId = profile.Id,
            DependencyId = priceProc.Id,
            SequenceNumber = 2
        });

        bikeMaterial.ConfigurationProfile = profile;
        Console.WriteLine($"  ✓ Created {profile.Procedures.Count} procedures");

        await context.SaveChangesAsync();
        Console.WriteLine("✓ Database setup complete!\n");
    }

    /// <summary>
    /// Prints available materials in the database
    /// </summary>
    public static async Task PrintAvailableMaterialsAsync(SqliteDbContext context)
    {
        var materials = await context.Materials
            .Include(m => m.ConfigurationProfile)
                .ThenInclude(p => p!.Procedures)
            .Include(m => m.ClassAssignments)
                .ThenInclude(ca => ca.Class)
                    .ThenInclude(c => c!.Characteristics)
            .ToListAsync();

        Console.WriteLine("=== Available Materials ===");
        foreach (var material in materials)
        {
            Console.WriteLine($"\nMaterial: {material.MaterialNumber} - {material.Description}");
            Console.WriteLine($"  Profile: {material.ConfigurationProfile?.Name ?? "None"}");
            Console.WriteLine($"  Procedures: {material.ConfigurationProfile?.Procedures.Count ?? 0}");

            foreach (var assignment in material.ClassAssignments)
            {
                Console.WriteLine($"  Class: {assignment.ClassName} ({assignment.Class?.Characteristics.Count ?? 0} characteristics)");
                if (assignment.Class != null)
                {
                    foreach (var ch in assignment.Class.Characteristics)
                    {
                        var required = ch.IsRequired ? "Required" : "Optional";
                        Console.WriteLine($"    - {ch.Name}: {ch.Description} ({required})");
                    }
                }
            }
        }
        Console.WriteLine();
    }
}
