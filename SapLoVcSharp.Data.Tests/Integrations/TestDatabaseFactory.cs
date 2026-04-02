using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Data.Models;
using SapLoVcSharp.Data.Serialization;
using SapLoVcSharp.Data.Sqlite;

namespace SapLoVcSharp.Data.Tests.Integrations
{
    /// <summary>
    /// Factory for creating in-memory SQLite databases with sample configuration data for testing
    /// </summary>
    public static class TestDatabaseFactory
    {
        /// <summary>
        /// Creates an in-memory SQLite database connection that stays open
        /// </summary>
        public static SqliteConnection CreateInMemoryConnection()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Creates a DbContext with an in-memory database
        /// </summary>
        public static SqliteDbContext CreateDbContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new SqliteDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Seeds the database with sample bike configuration data
        /// </summary>
        public static async Task SeedBikeConfigurationAsync(SqliteDbContext context)
        {
            var serializer = new AstSerializer();

            // Create characteristics class
            var bikeClass = new ClassEntity
            {
                Name = "BIKE_CLASS",
                ClassType = "300",
                Description = "Bicycle Configuration",
                Status = "Active"
            };

            // Create characteristics
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
                    new() { Value = "Green", Description = "Green" }
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

            bikeClass.Characteristics.Add(colorChar);
            bikeClass.Characteristics.Add(modelChar);
            bikeClass.Characteristics.Add(frameHeightChar);
            bikeClass.Characteristics.Add(gearCountChar);

            context.Classes.Add(bikeClass);

            // Create material
            var bikeMaterial = new MaterialEntity
            {
                MaterialNumber = "BIKE-001",
                Description = "Configurable Bicycle",
                Status = "Active"
            };

            // Assign class to material
            var classAssignment = new MaterialClassAssignmentEntity
            {
                MaterialNumber = "BIKE-001",
                ClassName = "BIKE_CLASS"
            };

            bikeMaterial.ClassAssignments.Add(classAssignment);
            context.Materials.Add(bikeMaterial);

            // Create configuration profile
            var profile = new ConfigurationProfileEntity
            {
                Id = "PROFILE-001",
                Name = "Bike Configuration Profile",
                MaterialNumber = "BIKE-001"
            };

            // Create a simple procedure: Set frame height default
            // Procedure: $SELF.FRAME_HEIGHT = 54
            var procedureDep = new DependencyEntity
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

            context.Dependencies.Add(procedureDep);

            // Add procedure to profile
            var profileProc = new ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = procedureDep.Id,
                SequenceNumber = 1
            };

            profile.Procedures.Add(profileProc);
            bikeMaterial.ConfigurationProfile = profile;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a complete test database with bike configuration
        /// </summary>
        public static async Task<(SqliteConnection Connection, SqliteDbContext Context)> CreateBikeConfigurationDatabaseAsync()
        {
            var connection = CreateInMemoryConnection();
            var context = CreateDbContext(connection);
            await SeedBikeConfigurationAsync(context);
            return (connection, context);
        }
    }
}
