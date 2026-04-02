using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Data.Models;
using SapLoVcSharp.Data.Sqlite;

namespace SapLoVcSharp.Data.Tests
{
    public class MaterialRepositoryTests : IDisposable
    {
        private readonly SqliteDbContext _context;

        public MaterialRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            _context = new SqliteDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task CreateMaterial_ShouldSaveToDatabase()
        {
            // Arrange
            var material = new MaterialEntity
            {
                MaterialNumber = "BIKE-001",
                Description = "Racing Bike",
                MaterialType = "KMAT",
                BaseUnit = "EA",
                Status = "Active"
            };

            // Act
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialNumber == "BIKE-001");

            retrieved.Should().NotBeNull();
            retrieved!.Description.Should().Be("Racing Bike");
        }

        [Fact]
        public async Task CreateMaterialWithConfigurationProfile_ShouldSaveRelationship()
        {
            // Arrange
            var material = new MaterialEntity
            {
                MaterialNumber = "BIKE-002",
                Description = "Mountain Bike",
                MaterialType = "KMAT"
            };

            var profile = new ConfigurationProfileEntity
            {
                Id = "PROFILE-001",
                Name = "Bike Configuration",
                MaterialNumber = "BIKE-002",
                Material = material
            };

            // Act
            _context.Materials.Add(material);
            _context.ConfigurationProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Assert
            var retrievedMaterial = await _context.Materials
                .Include(m => m.ConfigurationProfile)
                .FirstOrDefaultAsync(m => m.MaterialNumber == "BIKE-002");

            retrievedMaterial.Should().NotBeNull();
            retrievedMaterial!.ConfigurationProfile.Should().NotBeNull();
            retrievedMaterial.ConfigurationProfile!.Name.Should().Be("Bike Configuration");
        }

        [Fact]
        public async Task AssignClassToMaterial_ShouldCreateAssignment()
        {
            // Arrange
            var material = new MaterialEntity
            {
                MaterialNumber = "BIKE-003",
                Description = "City Bike",
                MaterialType = "KMAT"
            };

            var bikeClass = new ClassEntity
            {
                Name = "BIKE_CLASS",                    // Changed: Name is now PK
                Description = "Bicycle Class",
                ClassType = "300"
            };

            var assignment = new MaterialClassAssignmentEntity
            {
                MaterialNumber = "BIKE-003",
                ClassName = "BIKE_CLASS"                // Changed: Use ClassName instead of ClassId
            };

            // Act
            _context.Materials.Add(material);
            _context.Classes.Add(bikeClass);
            _context.MaterialClassAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Assert
            var retrievedMaterial = await _context.Materials
                .Include(m => m.ClassAssignments)
                    .ThenInclude(a => a.Class)
                .FirstOrDefaultAsync(m => m.MaterialNumber == "BIKE-003");

            retrievedMaterial.Should().NotBeNull();
            retrievedMaterial!.ClassAssignments.Should().HaveCount(1);
            retrievedMaterial.ClassAssignments[0].Class!.Name.Should().Be("BIKE_CLASS");
        }

        [Fact]
        public async Task CreateClassWithCharacteristics_ShouldSaveHierarchy()
        {
            // Arrange
            var bikeClass = new ClassEntity
            {
                Name = "BIKE_CLASS",                    // Changed: Name is now PK
                Description = "Bicycle Class",
                ClassType = "300"
            };

            var colorChar = new CharacteristicEntity
            {
                ClassName = "BIKE_CLASS",               // Changed: Composite key
                Name = "COLOR",                         // Changed: Name is part of PK
                Description = "Bike Color",
                DataType = "CHAR",
                Length = 30
            };

            var redValue = new CharacteristicValueEntity
            {
                ClassName = "BIKE_CLASS",               // Changed: Composite key
                CharacteristicName = "COLOR",           // Changed: Part of composite key
                Value = "RED",                          // Changed: Part of composite key
                Description = "Red Color"
            };

            var blueValue = new CharacteristicValueEntity
            {
                ClassName = "BIKE_CLASS",               // Changed: Composite key
                CharacteristicName = "COLOR",           // Changed: Part of composite key
                Value = "BLUE",                         // Changed: Part of composite key
                Description = "Blue Color"
            };

            // Act
            _context.Classes.Add(bikeClass);
            _context.Characteristics.Add(colorChar);
            _context.CharacteristicValues.Add(redValue);
            _context.CharacteristicValues.Add(blueValue);
            await _context.SaveChangesAsync();

            // Assert
            var retrievedClass = await _context.Classes
                .Include(c => c.Characteristics)
                    .ThenInclude(ch => ch.Values)
                .FirstOrDefaultAsync(c => c.Name == "BIKE_CLASS");

            retrievedClass.Should().NotBeNull();
            retrievedClass!.Characteristics.Should().HaveCount(1);
            retrievedClass.Characteristics[0].Values.Should().HaveCount(2);
        }

        [Fact]
        public async Task DeleteMaterial_ShouldCascadeDeleteProfile()
        {
            // Arrange
            var material = new MaterialEntity
            {
                MaterialNumber = "BIKE-004",
                Description = "Test Bike",
                MaterialType = "KMAT"
            };

            var profile = new ConfigurationProfileEntity
            {
                Id = "PROFILE-002",
                Name = "Test Profile",
                MaterialNumber = "BIKE-004"
            };

            _context.Materials.Add(material);
            _context.ConfigurationProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Act
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            // Assert
            var retrievedProfile = await _context.ConfigurationProfiles
                .FirstOrDefaultAsync(p => p.Id == "PROFILE-002");

            retrievedProfile.Should().BeNull(); // Should be cascade deleted
        }

        [Fact]
        public async Task CreateDependencyNet_WithConstraints_ShouldSaveRelationships()
        {
            // Arrange
            var material = new MaterialEntity
            {
                MaterialNumber = "BIKE-005",
                Description = "Test Bike",
                MaterialType = "KMAT"
            };

            var profile = new ConfigurationProfileEntity
            {
                Id = "PROFILE-003",
                Name = "Test Profile",
                MaterialNumber = "BIKE-005"
            };

            var dependencyNet = new DependencyNetEntity
            {
                Id = "NET-001",
                Name = "Color Constraints",
                ConfigurationProfileId = "PROFILE-003"
            };

            var constraint = new DependencyEntity
            {
                Id = "CONST-001",
                Name = "Color Rule",
                Type = "Constraint",
                SourceCode = "OBJECTS: PC IS_A (300) BIKE WHERE COLOR = COLOR. RESTRICTIONS: COLOR = 'Red'.",
                AstJson = "{}"
            };

            var netConstraint = new DependencyNetConstraintEntity
            {
                DependencyNetId = "NET-001",
                DependencyId = "CONST-001",
                SequenceNumber = 1
            };

            // Act
            _context.Materials.Add(material);
            _context.ConfigurationProfiles.Add(profile);
            _context.DependencyNets.Add(dependencyNet);
            _context.Dependencies.Add(constraint);
            _context.DependencyNetConstraints.Add(netConstraint);
            await _context.SaveChangesAsync();

            // Assert
            var retrievedNet = await _context.DependencyNets
                .Include(n => n.Constraints)
                    .ThenInclude(c => c.Dependency)
                .FirstOrDefaultAsync(n => n.Id == "NET-001");

            retrievedNet.Should().NotBeNull();
            retrievedNet!.Constraints.Should().HaveCount(1);
            retrievedNet.Constraints[0].Dependency!.Name.Should().Be("Color Rule");
        }

        public void Dispose()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }
    }
}