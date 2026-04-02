using FluentAssertions;
using SapLoVcSharp.Data.Sqlite.Services;
using System.Text.Json;

namespace SapLoVcSharp.Data.Tests.Integrations
{
    /// <summary>
    /// Integration tests for ExecutionService with real SQLite database
    /// </summary>
    public class ExecutionServiceTests : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly SapLoVcSharp.Data.Sqlite.SqliteDbContext _context;

        public ExecutionServiceTests()
        {
            _connection = TestDatabaseFactory.CreateInMemoryConnection();
            _context = TestDatabaseFactory.CreateDbContext(_connection);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        [Fact]
        public async Task ExecuteConfiguration_WithBikeData_ShouldSucceed()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.MaterialNumber.Should().Be("BIKE-001");
        }

        [Fact]
        public async Task ExecuteConfiguration_ShouldExecuteProcedure()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // Check that frame height was set by the procedure
            result.FinalValues.Should().ContainKey("FRAME_HEIGHT");
            // Extract value from JsonElement if needed
            var frameHeight = GetNumericValue(result.FinalValues["FRAME_HEIGHT"]);
            frameHeight.Should().Be(54);
        }

        [Fact]
        public async Task ExecuteConfiguration_WithDefaults_ShouldInitializeDefaultValues()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // Check that default values were set
            result.FinalValues.Should().ContainKey("COLOR");
            GetStringValue(result.FinalValues["COLOR"]).Should().Be("Red"); // Default color

            result.FinalValues.Should().ContainKey("MODEL");
            GetStringValue(result.FinalValues["MODEL"]).Should().Be("City"); // Default model
        }

        [Fact]
        public async Task ExecuteConfiguration_ShouldIncludeBothDefaultsAndProcedureValues()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // Should have both default values and procedure-set values
            result.FinalValues.Should().ContainKey("COLOR"); // Default
            result.FinalValues.Should().ContainKey("FRAME_HEIGHT"); // From procedure
        }

        [Fact]
        public async Task ExecuteConfiguration_ShouldLogExecution()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.ExecutionLogs.Should().NotBeEmpty();

            // With cycle execution, dependencies may execute multiple times
            var procedureLogs = result.ExecutionLogs.Where(log => log.DependencyName == "SetDefaultFrameHeight").ToList();
            procedureLogs.Should().NotBeEmpty("procedure should be executed at least once");
            procedureLogs.First().DependencyType.Should().Be("Procedure");
        }

        [Fact]
        public async Task ExecuteConfiguration_ShouldTrackMissingRequired()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act - Execute without setting required characteristics
            var result = await executionService.ExecuteConfigurationAsync("BIKE-001");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.IsComplete.Should().BeTrue(); // Should be complete because defaults are set

            // With defaults set, no required characteristics should be missing
            result.MissingRequired.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteConfiguration_InvalidMaterial_ShouldFail()
        {
            // Arrange
            await TestDatabaseFactory.SeedBikeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("INVALID-MATERIAL");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExecuteConfiguration_WithMultipleConstraintsAndProcedures_ShouldExecuteCyclesCorrectly()
        {
            // Arrange - Create a complex configuration with 2 procedures and 5 constraints
            // that have dependencies on each other, requiring multiple cycles
            await SeedProductConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("PRODUCT-001");

            // Assert - Verify cycle execution
            result.Should().NotBeNull();
            result.Success.Should().BeTrue("configuration should execute successfully");
            result.IsStable.Should().BeTrue("configuration should reach a stable state");
            result.CyclesExecuted.Should().BeGreaterThan(1, "multiple cycles should be required for dependent constraints");
            result.CyclesExecuted.Should().BeLessThanOrEqualTo(20, "should not exceed max cycles");

            // Verify cycle details
            result.CycleDetails.Should().NotBeEmpty();
            result.CycleDetails.Count.Should().Be(result.CyclesExecuted);

            // First cycle should have changes
            var firstCycle = result.CycleDetails.First();
            firstCycle.CycleNumber.Should().Be(1);
            firstCycle.ChangedCharacteristics.Should().NotBeEmpty("first cycle should set initial values");
            firstCycle.ProceduresExecuted.Should().Be(2, "should execute 2 procedures");
            firstCycle.ConstraintsExecuted.Should().Be(5, "should execute 5 constraints");

            // Last cycle should be stable
            var lastCycle = result.CycleDetails.Last();
            lastCycle.IsStable.Should().BeTrue("last cycle should be stable");
            lastCycle.ChangedCharacteristics.Should().BeEmpty("stable cycle should have no changes");

            // Verify the dependency chain executed correctly
            result.FinalValues.Should().ContainKey("PRODUCT_TYPE");
            GetStringValue(result.FinalValues["PRODUCT_TYPE"]).Should().Be("LAPTOP", "procedure 1 should set PRODUCT_TYPE");

            result.FinalValues.Should().ContainKey("QUALITY");
            GetStringValue(result.FinalValues["QUALITY"]).Should().Be("PREMIUM", "procedure 2 should set QUALITY");

            result.FinalValues.Should().ContainKey("CATEGORY");
            GetStringValue(result.FinalValues["CATEGORY"]).Should().Be("ELECTRONICS", "constraint 1 should set CATEGORY based on PRODUCT_TYPE");

            result.FinalValues.Should().ContainKey("WARRANTY");
            GetStringValue(result.FinalValues["WARRANTY"]).Should().Be("2_YEARS", "constraint 2 should set WARRANTY based on CATEGORY");

            result.FinalValues.Should().ContainKey("SUPPORT");
            GetStringValue(result.FinalValues["SUPPORT"]).Should().Be("EXTENDED", "constraint 3 should set SUPPORT based on WARRANTY and QUALITY");

            result.FinalValues.Should().ContainKey("PRICE_TIER");
            GetStringValue(result.FinalValues["PRICE_TIER"]).Should().Be("HIGH", "constraint 4 should set PRICE_TIER based on SUPPORT");

            result.FinalValues.Should().ContainKey("SHIPPING");
            GetStringValue(result.FinalValues["SHIPPING"]).Should().Be("EXPRESS", "constraint 5 should set SHIPPING based on PRICE_TIER");

            // Verify all dependencies executed
            result.ExecutionLogs.Should().NotBeEmpty();
            var procedureLogs = result.ExecutionLogs.Where(log => log.DependencyType == "Procedure").ToList();
            var constraintLogs = result.ExecutionLogs.Where(log => log.DependencyType == "Constraint").ToList();

            // Each procedure and constraint should execute at least once per cycle
            procedureLogs.Count.Should().BeGreaterThanOrEqualTo(2 * result.CyclesExecuted, "2 procedures × cycles");
            constraintLogs.Count.Should().BeGreaterThanOrEqualTo(5 * result.CyclesExecuted, "5 constraints × cycles");

            // Verify changed characteristics tracked correctly
            result.ChangedCharacteristics.Should().Contain(new[] {
                "PRODUCT_TYPE", "QUALITY", "CATEGORY", "WARRANTY", "SUPPORT", "PRICE_TIER", "SHIPPING"
            }, "all characteristics in the dependency chain should be tracked as changed");

            Console.WriteLine($"\n=== Cycle Execution Test Results ===");
            Console.WriteLine($"Cycles Executed: {result.CyclesExecuted}");
            Console.WriteLine($"Stable: {result.IsStable}");
            Console.WriteLine($"Dependencies Executed: {result.ExecutionLogs.Count}");
            Console.WriteLine($"Changed Characteristics: {string.Join(", ", result.ChangedCharacteristics)}");

            foreach (var cycle in result.CycleDetails)
            {
                Console.WriteLine($"Cycle {cycle.CycleNumber}: {cycle.TotalDependenciesExecuted} deps, " +
                    $"{cycle.ChangedCharacteristics.Count} changes, {cycle.DurationMs}ms");
            }
        }

        private async Task SeedProductConfigurationAsync(SapLoVcSharp.Data.Sqlite.SqliteDbContext context)
        {
            var serializer = new SapLoVcSharp.Data.Serialization.AstSerializer();

            // Create class with characteristics
            var productClass = new SapLoVcSharp.Data.Models.ClassEntity
            {
                Name = "CL_PRODUCT",
                ClassType = "300",
                Description = "Product Configuration Class",
                Status = "Active"
            };

            // Add characteristics
            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "PRODUCT_TYPE",
                Description = "Product Type",
                DataType = "CHAR",
                IsRequired = true,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "LAPTOP" },
                    new() { Value = "DESKTOP" },
                    new() { Value = "TABLET" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "QUALITY",
                Description = "Quality Level",
                DataType = "CHAR",
                IsRequired = true,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "STANDARD" },
                    new() { Value = "PREMIUM" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "CATEGORY",
                Description = "Product Category",
                DataType = "CHAR",
                IsRequired = false,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "ELECTRONICS" },
                    new() { Value = "APPLIANCES" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "WARRANTY",
                Description = "Warranty Period",
                DataType = "CHAR",
                IsRequired = false,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "1_YEAR" },
                    new() { Value = "2_YEARS" },
                    new() { Value = "3_YEARS" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "SUPPORT",
                Description = "Support Level",
                DataType = "CHAR",
                IsRequired = false,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "BASIC" },
                    new() { Value = "EXTENDED" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "PRICE_TIER",
                Description = "Price Tier",
                DataType = "CHAR",
                IsRequired = false,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "LOW" },
                    new() { Value = "MEDIUM" },
                    new() { Value = "HIGH" }
                }
            });

            productClass.Characteristics.Add(new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "CL_PRODUCT",
                Name = "SHIPPING",
                Description = "Shipping Method",
                DataType = "CHAR",
                IsRequired = false,
                Values = new List<SapLoVcSharp.Data.Models.CharacteristicValueEntity>
                {
                    new() { Value = "STANDARD" },
                    new() { Value = "EXPRESS" },
                    new() { Value = "OVERNIGHT" }
                }
            });

            context.Classes.Add(productClass);

            // Create material
            var product = new SapLoVcSharp.Data.Models.MaterialEntity
            {
                MaterialNumber = "PRODUCT-001",
                Description = "Configurable Product",
                Status = "Active"
            };

            product.ClassAssignments.Add(new SapLoVcSharp.Data.Models.MaterialClassAssignmentEntity
            {
                MaterialNumber = "PRODUCT-001",
                ClassName = "CL_PRODUCT"
            });

            context.Materials.Add(product);

            // Create configuration profile
            var profile = new SapLoVcSharp.Data.Models.ConfigurationProfileEntity
            {
                Id = "PROFILE_PRODUCT",
                Name = "Product Configuration Profile",
                MaterialNumber = "PRODUCT-001"
            };

            // Create Procedure 1: Set PRODUCT_TYPE = 'LAPTOP'
            var proc1Source = "$SELF.PRODUCT_TYPE = 'LAPTOP'";
            var proc1 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "PROC_SET_PRODUCT_TYPE",
                Name = "SetProductType",
                Type = "Procedure",
                SourceCode = proc1Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ProcedureParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(proc1Source).Tokenize()
                    ).Parse()
                )
            };

            // Create Procedure 2: Set QUALITY = 'PREMIUM'
            var proc2Source = "$SELF.QUALITY = 'PREMIUM'";
            var proc2 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "PROC_SET_QUALITY",
                Name = "SetQuality",
                Type = "Procedure",
                SourceCode = proc2Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ProcedureParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(proc2Source).Tokenize()
                    ).Parse()
                )
            };

            context.Dependencies.Add(proc1);
            context.Dependencies.Add(proc2);

            profile.Procedures.Add(new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = proc1.Id,
                SequenceNumber = 1
            });

            profile.Procedures.Add(new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = proc2.Id,
                SequenceNumber = 2
            });

            // Create Constraint 1: If PRODUCT_TYPE = 'LAPTOP', set CATEGORY = 'ELECTRONICS'
            var constr1Source = @"
                OBJECTS:
                PROD IS_A (300) CL_PRODUCT WHERE
                    lv_PRODUCT_TYPE = PRODUCT_TYPE;
                    lv_CATEGORY = CATEGORY

                CONDITION:
                lv_PRODUCT_TYPE = 'LAPTOP'

                RESTRICTIONS:
                lv_CATEGORY = 'ELECTRONICS'

                INFERENCES:
                lv_CATEGORY";

            var constr1 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_CATEGORY",
                Name = "SetCategory",
                Type = "Constraint",
                SourceCode = constr1Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr1Source).Tokenize()
                    ).Parse()
                )
            };

            // Create Constraint 2: If CATEGORY = 'ELECTRONICS', set WARRANTY = '2_YEARS'
            var constr2Source = @"
                OBJECTS:
                PROD IS_A (300) CL_PRODUCT WHERE
                    lv_CATEGORY = CATEGORY;
                    lv_WARRANTY = WARRANTY

                CONDITION:
                lv_CATEGORY = 'ELECTRONICS'

                RESTRICTIONS:
                lv_WARRANTY = '2_YEARS'

                INFERENCES:
                lv_WARRANTY";

            var constr2 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_WARRANTY",
                Name = "SetWarranty",
                Type = "Constraint",
                SourceCode = constr2Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr2Source).Tokenize()
                    ).Parse()
                )
            };

            // Create Constraint 3: If WARRANTY = '2_YEARS' AND QUALITY = 'PREMIUM', set SUPPORT = 'EXTENDED'
            var constr3Source = @"
                OBJECTS:
                PROD IS_A (300) CL_PRODUCT WHERE
                    lv_WARRANTY = WARRANTY;
                    lv_QUALITY = QUALITY;
                    lv_SUPPORT = SUPPORT

                CONDITION:
                lv_WARRANTY = '2_YEARS' AND lv_QUALITY = 'PREMIUM'

                RESTRICTIONS:
                lv_SUPPORT = 'EXTENDED'

                INFERENCES:
                lv_SUPPORT";

            var constr3 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_SUPPORT",
                Name = "SetSupport",
                Type = "Constraint",
                SourceCode = constr3Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr3Source).Tokenize()
                    ).Parse()
                )
            };

            // Create Constraint 4: If SUPPORT = 'EXTENDED', set PRICE_TIER = 'HIGH'
            var constr4Source = @"
                OBJECTS:
                PROD IS_A (300) CL_PRODUCT WHERE
                    lv_SUPPORT = SUPPORT;
                    lv_PRICE_TIER = PRICE_TIER

                CONDITION:
                lv_SUPPORT = 'EXTENDED'

                RESTRICTIONS:
                lv_PRICE_TIER = 'HIGH'

                INFERENCES:
                lv_PRICE_TIER";

            var constr4 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_PRICE_TIER",
                Name = "SetPriceTier",
                Type = "Constraint",
                SourceCode = constr4Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr4Source).Tokenize()
                    ).Parse()
                )
            };

            // Create Constraint 5: If PRICE_TIER = 'HIGH', set SHIPPING = 'EXPRESS'
            var constr5Source = @"
                OBJECTS:
                PROD IS_A (300) CL_PRODUCT WHERE
                    lv_PRICE_TIER = PRICE_TIER;
                    lv_SHIPPING = SHIPPING

                CONDITION:
                lv_PRICE_TIER = 'HIGH'

                RESTRICTIONS:
                lv_SHIPPING = 'EXPRESS'

                INFERENCES:
                lv_SHIPPING";

            var constr5 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_SHIPPING",
                Name = "SetShipping",
                Type = "Constraint",
                SourceCode = constr5Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr5Source).Tokenize()
                    ).Parse()
                )
            };

            context.Dependencies.Add(constr1);
            context.Dependencies.Add(constr2);
            context.Dependencies.Add(constr3);
            context.Dependencies.Add(constr4);
            context.Dependencies.Add(constr5);

            // Create dependency net with all 5 constraints
            var dependencyNet = new SapLoVcSharp.Data.Models.DependencyNetEntity
            {
                Id = "NET_PRODUCT_CONFIG",
                Name = "Product Configuration Net",
                Description = "Net with 5 interdependent constraints",
                ConfigurationProfileId = profile.Id
            };

            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr1.Id,
                SequenceNumber = 1
            });

            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr2.Id,
                SequenceNumber = 2
            });

            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr3.Id,
                SequenceNumber = 3
            });

            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr4.Id,
                SequenceNumber = 4
            });

            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr5.Id,
                SequenceNumber = 5
            });

            profile.DependencyNets.Add(dependencyNet);
            product.ConfigurationProfile = profile;

            context.ConfigurationProfiles.Add(profile);

            await context.SaveChangesAsync();
        }

        private static int GetNumericValue(object? value)
        {
            if (value is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }
            return Convert.ToInt32(value);
        }

        private static string? GetStringValue(object? value)
        {
            if (value is JsonElement jsonElement)
            {
                return jsonElement.GetString();
            }
            return value?.ToString();
        }

        [Fact]
        public async Task ExecuteConfiguration_WithTableCallsInRestrictions_ShouldExecuteCyclesCorrectly()
        {
            // Arrange - Create configuration with 5 constraints using TABLE calls in RESTRICTIONS
            // Some constraints share the same table
            await SeedTableBasedConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            // Act
            var result = await executionService.ExecuteConfigurationAsync("PRODUCT-TABLE-001");

            // Output debug information first
            Console.WriteLine($"\n=== Table-Based Cycle Execution Test Results ===");
            Console.WriteLine($"Cycles Executed: {result.CyclesExecuted}");
            Console.WriteLine($"Stable: {result.IsStable}");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Dependencies Executed: {result.ExecutionLogs.Count}");
            Console.WriteLine($"Changed Characteristics: {string.Join(", ", result.ChangedCharacteristics)}");

            Console.WriteLine($"\n=== Final Values ===");
            foreach (var (key, value) in result.FinalValues)
            {
                Console.WriteLine($"{key} = {GetStringValue(value)}");
            }

            Console.WriteLine($"\n=== Cycle Details ===");
            foreach (var cycle in result.CycleDetails)
            {
                Console.WriteLine($"Cycle {cycle.CycleNumber}: {cycle.TotalDependenciesExecuted} deps, " +
                    $"{cycle.ChangedCharacteristics.Count} changes, {cycle.DurationMs}ms");
                Console.WriteLine($"  Changed: {string.Join(", ", cycle.ChangedCharacteristics)}");
            }

            Console.WriteLine($"\n=== Execution Logs ===");
            foreach (var log in result.ExecutionLogs)
            {
                Console.WriteLine($"[{log.DependencyType}] {log.DependencyName}: Success={log.Success}, Duration={log.DurationMs}ms");
                if (!log.Success && !string.IsNullOrEmpty(log.ErrorDetails))
                {
                    Console.WriteLine($"  ERROR: {log.ErrorDetails}");
                }
                if (!string.IsNullOrEmpty(log.Message))
                {
                    Console.WriteLine($"  MESSAGE: {log.Message}");
                }
            }

            // Assert - Verify cycle execution
            result.Should().NotBeNull();
            result.Success.Should().BeTrue("configuration should execute successfully");
            result.IsStable.Should().BeTrue("configuration should reach a stable state");
            result.CyclesExecuted.Should().BeGreaterThan(1, "multiple cycles should be required for dependent constraints");
            result.CyclesExecuted.Should().BeLessThanOrEqualTo(20, "should not exceed max cycles");

            // Verify cycle details
            result.CycleDetails.Should().NotBeEmpty();
            result.CycleDetails.Count.Should().Be(result.CyclesExecuted);

            // First cycle should have changes
            var firstCycle = result.CycleDetails.First();
            firstCycle.CycleNumber.Should().Be(1);
            firstCycle.ChangedCharacteristics.Should().NotBeEmpty("first cycle should set initial values");
            firstCycle.ProceduresExecuted.Should().Be(2, "should execute 2 procedures");
            firstCycle.ConstraintsExecuted.Should().Be(5, "should execute 5 constraints");

            // Last cycle should be stable
            var lastCycle = result.CycleDetails.Last();
            lastCycle.IsStable.Should().BeTrue("last cycle should be stable");
            lastCycle.ChangedCharacteristics.Should().BeEmpty("stable cycle should have no changes");

            // Verify the dependency chain executed correctly with table lookups
            result.FinalValues.Should().ContainKey("PRODUCT_TYPE");
            GetStringValue(result.FinalValues["PRODUCT_TYPE"]).Should().Be("LAPTOP", "procedure 1 should set PRODUCT_TYPE");

            result.FinalValues.Should().ContainKey("REGION");
            GetStringValue(result.FinalValues["REGION"]).Should().Be("EU", "procedure 2 should set REGION");

            result.FinalValues.Should().ContainKey("CATEGORY");
            GetStringValue(result.FinalValues["CATEGORY"]).Should().Be("ELECTRONICS",
                "constraint 1 should lookup CATEGORY='ELECTRONICS' from T_PRODUCT_SPECS where PRODUCT_TYPE='LAPTOP'");

            result.FinalValues.Should().ContainKey("WARRANTY");
            GetStringValue(result.FinalValues["WARRANTY"]).Should().Be("3_YEARS",
                "constraint 2 should lookup WARRANTY='3_YEARS' from T_WARRANTY_RULES where CATEGORY='ELECTRONICS' and REGION='EU'");

            result.FinalValues.Should().ContainKey("SUPPORT");
            GetStringValue(result.FinalValues["SUPPORT"]).Should().Be("PREMIUM",
                "constraint 3 should lookup SUPPORT='PREMIUM' from T_WARRANTY_RULES (shared table) where WARRANTY='3_YEARS'");

            result.FinalValues.Should().ContainKey("PRICE_TIER");
            GetStringValue(result.FinalValues["PRICE_TIER"]).Should().Be("HIGH",
                "constraint 4 should lookup PRICE_TIER='HIGH' from T_PRICING where CATEGORY='ELECTRONICS' and REGION='EU'");

            result.FinalValues.Should().ContainKey("SHIPPING");
            GetStringValue(result.FinalValues["SHIPPING"]).Should().Be("STANDARD",
                "constraint 5 should lookup SHIPPING='STANDARD' from T_SHIPPING where PRICE_TIER='HIGH' and REGION='EU'");

            // Verify all dependencies executed
            result.ExecutionLogs.Should().NotBeEmpty();
            var procedureLogs = result.ExecutionLogs.Where(log => log.DependencyType == "Procedure").ToList();
            var constraintLogs = result.ExecutionLogs.Where(log => log.DependencyType == "Constraint").ToList();

            // Each procedure and constraint should execute at least once per cycle
            procedureLogs.Count.Should().BeGreaterThanOrEqualTo(2 * result.CyclesExecuted, "2 procedures × cycles");
            constraintLogs.Count.Should().BeGreaterThanOrEqualTo(5 * result.CyclesExecuted, "5 constraints × cycles");

            // Verify changed characteristics tracked correctly
            result.ChangedCharacteristics.Should().Contain(new[] {
                "PRODUCT_TYPE", "REGION", "CATEGORY", "WARRANTY", "SUPPORT", "PRICE_TIER", "SHIPPING"
            }, "all characteristics in the dependency chain should be tracked as changed");
        }

        private async Task SeedTableBasedConfigurationAsync(SapLoVcSharp.Data.Sqlite.SqliteDbContext context)
        {
            var serializer = new SapLoVcSharp.Data.Serialization.AstSerializer();

            // Create class with 7 characteristics
            var productClass = new SapLoVcSharp.Data.Models.ClassEntity
            {
                Name = "CL_TABLE_PRODUCT",
                ClassType = "300",
                Description = "Product Configuration Class with Table Lookups",
                Status = "Active"
            };

            // Add characteristics with values
            var charDefs = new[]
            {
                ("PRODUCT_TYPE", "Type of product", new[] { "LAPTOP", "PHONE", "DESK" }),
                ("REGION", "Sales region", new[] { "EU", "US" }),
                ("CATEGORY", "Product category", new[] { "ELECTRONICS", "FURNITURE" }),
                ("WARRANTY", "Warranty period", new[] { "1_YEAR", "2_YEARS", "3_YEARS", "5_YEARS" }),
                ("SUPPORT", "Support level", new[] { "BASIC", "STANDARD", "PREMIUM", "EXTENDED" }),
                ("PRICE_TIER", "Pricing tier", new[] { "LOW", "MEDIUM", "HIGH" }),
                ("SHIPPING", "Shipping method", new[] { "ECONOMY", "STANDARD", "EXPRESS" })
            };

            foreach (var (name, desc, values) in charDefs)
            {
                var characteristic = new SapLoVcSharp.Data.Models.CharacteristicEntity
                {
                    ClassName = productClass.Name,
                    Name = name,
                    Description = desc,
                    DataType = "CHAR",
                    IsRequired = false
                };

                foreach (var value in values)
                {
                    characteristic.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
                    {
                        Value = value
                    });
                }

                productClass.Characteristics.Add(characteristic);
            }

            context.Classes.Add(productClass);

            // Create variant tables using helper method

            // Table 1: T_PRODUCT_SPECS - Maps PRODUCT_TYPE to CATEGORY
            await CreateVariantTableAsync(
                context,
                "T_PRODUCT_SPECS",
                "Product specifications lookup",
                new[] { "PRODUCT_TYPE", "CATEGORY" },
                new[] { true, false },
                new[]
                {
                    new[] { "LAPTOP", "ELECTRONICS" },
                    new[] { "PHONE", "ELECTRONICS" },
                    new[] { "DESK", "FURNITURE" }
                });

            // Table 2: T_WARRANTY_RULES - Maps CATEGORY+REGION to WARRANTY, and WARRANTY to SUPPORT
            // This table is SHARED between constraint 2 and constraint 3
            await CreateVariantTableAsync(
                context,
                "T_WARRANTY_RULES",
                "Warranty and support rules (shared table)",
                new[] { "CATEGORY", "REGION", "WARRANTY", "SUPPORT" },
                new[] { false, false, false, false },
                new[]
                {
                    new[] { "ELECTRONICS", "EU", "3_YEARS", "PREMIUM" },
                    new[] { "ELECTRONICS", "US", "2_YEARS", "STANDARD" },
                    new[] { "FURNITURE", "EU", "5_YEARS", "EXTENDED" }
                });

            // Table 3: T_PRICING - Maps CATEGORY+REGION to PRICE_TIER
            await CreateVariantTableAsync(
                context,
                "T_PRICING",
                "Pricing tier lookup",
                new[] { "CATEGORY", "REGION", "PRICE_TIER" },
                new[] { false, false, false },
                new[]
                {
                    new[] { "ELECTRONICS", "EU", "HIGH" },
                    new[] { "ELECTRONICS", "US", "MEDIUM" },
                    new[] { "FURNITURE", "EU", "MEDIUM" }
                });

            // Table 4: T_SHIPPING - Maps PRICE_TIER+REGION to SHIPPING
            await CreateVariantTableAsync(
                context,
                "T_SHIPPING",
                "Shipping method lookup",
                new[] { "PRICE_TIER", "REGION", "SHIPPING" },
                new[] { false, false, false },
                new[]
                {
                    new[] { "HIGH", "EU", "STANDARD" },
                    new[] { "HIGH", "US", "EXPRESS" },
                    new[] { "MEDIUM", "EU", "ECONOMY" }
                });

            // Create material
            var material = new SapLoVcSharp.Data.Models.MaterialEntity
            {
                MaterialNumber = "PRODUCT-TABLE-001",
                Description = "Table-based configurable product",
                Status = "Active"
            };

            // Assign class to material
            material.ClassAssignments.Add(new SapLoVcSharp.Data.Models.MaterialClassAssignmentEntity
            {
                MaterialNumber = material.MaterialNumber,
                ClassName = productClass.Name
            });

            context.Materials.Add(material);

            // Create 2 procedures
            var proc1Source = "$SELF.PRODUCT_TYPE = 'LAPTOP'";
            var proc1 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "PROC_SET_PRODUCT_TYPE",
                Name = "SetProductType",
                Type = "Procedure",
                SourceCode = proc1Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ProcedureParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(proc1Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(proc1);

            var proc2Source = "$SELF.REGION = 'EU'";
            var proc2 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "PROC_SET_REGION",
                Name = "SetRegion",
                Type = "Procedure",
                SourceCode = proc2Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ProcedureParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(proc2Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(proc2);

            // Create 5 constraints with TABLE calls in RESTRICTIONS

            // Constraint 1: Lookup CATEGORY from T_PRODUCT_SPECS based on PRODUCT_TYPE
            var constr1Source = @"
OBJECTS:
PROD IS_A (300) CL_TABLE_PRODUCT WHERE
    lv_PRODUCT_TYPE = PRODUCT_TYPE;
    lv_CATEGORY = CATEGORY

CONDITION:
lv_PRODUCT_TYPE = 'LAPTOP'

RESTRICTIONS:
TABLE T_PRODUCT_SPECS (
    PRODUCT_TYPE = lv_PRODUCT_TYPE,
    CATEGORY ?= lv_CATEGORY
)

INFERENCES:
lv_CATEGORY";

            var constr1 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_LOOKUP_CATEGORY",
                Name = "LookupCategory",
                Type = "Constraint",
                SourceCode = constr1Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr1Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constr1);

            // Constraint 2: Lookup WARRANTY from T_WARRANTY_RULES based on CATEGORY and REGION
            var constr2Source = @"
OBJECTS:
PROD IS_A (300) CL_TABLE_PRODUCT WHERE
    lv_CATEGORY = CATEGORY;
    lv_REGION = REGION;
    lv_WARRANTY = WARRANTY

CONDITION:
lv_CATEGORY = 'ELECTRONICS' AND lv_REGION = 'EU'

RESTRICTIONS:
TABLE T_WARRANTY_RULES (
    CATEGORY = lv_CATEGORY,
    REGION = lv_REGION,
    WARRANTY ?= lv_WARRANTY
)

INFERENCES:
lv_WARRANTY";

            var constr2 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_LOOKUP_WARRANTY",
                Name = "LookupWarranty",
                Type = "Constraint",
                SourceCode = constr2Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr2Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constr2);

            // Constraint 3: Lookup SUPPORT from T_WARRANTY_RULES (same table as constraint 2) based on WARRANTY
            var constr3Source = @"
OBJECTS:
PROD IS_A (300) CL_TABLE_PRODUCT WHERE
    lv_WARRANTY = WARRANTY;
    lv_SUPPORT = SUPPORT

CONDITION:
lv_WARRANTY = '3_YEARS'

RESTRICTIONS:
TABLE T_WARRANTY_RULES (
    WARRANTY = lv_WARRANTY,
    SUPPORT ?= lv_SUPPORT
)

INFERENCES:
lv_SUPPORT";

            var constr3 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_LOOKUP_SUPPORT",
                Name = "LookupSupport",
                Type = "Constraint",
                SourceCode = constr3Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr3Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constr3);

            // Constraint 4: Lookup PRICE_TIER from T_PRICING based on CATEGORY and REGION
            var constr4Source = @"
OBJECTS:
PROD IS_A (300) CL_TABLE_PRODUCT WHERE
    lv_CATEGORY = CATEGORY;
    lv_REGION = REGION;
    lv_PRICE_TIER = PRICE_TIER

CONDITION:
lv_CATEGORY = 'ELECTRONICS' AND lv_REGION = 'EU'

RESTRICTIONS:
TABLE T_PRICING (
    CATEGORY = lv_CATEGORY,
    REGION = lv_REGION,
    PRICE_TIER ?= lv_PRICE_TIER
)

INFERENCES:
lv_PRICE_TIER";

            var constr4 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_LOOKUP_PRICE",
                Name = "LookupPriceTier",
                Type = "Constraint",
                SourceCode = constr4Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr4Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constr4);

            // Constraint 5: Lookup SHIPPING from T_SHIPPING based on PRICE_TIER and REGION
            var constr5Source = @"
OBJECTS:
PROD IS_A (300) CL_TABLE_PRODUCT WHERE
    lv_PRICE_TIER = PRICE_TIER;
    lv_REGION = REGION;
    lv_SHIPPING = SHIPPING

CONDITION:
lv_PRICE_TIER = 'HIGH' AND lv_REGION = 'EU'

RESTRICTIONS:
TABLE T_SHIPPING (
    PRICE_TIER = lv_PRICE_TIER,
    REGION = lv_REGION,
    SHIPPING ?= lv_SHIPPING
)

INFERENCES:
lv_SHIPPING";

            var constr5 = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_LOOKUP_SHIPPING",
                Name = "LookupShipping",
                Type = "Constraint",
                SourceCode = constr5Source,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constr5Source).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constr5);

            // Create configuration profile
            var profile = new SapLoVcSharp.Data.Models.ConfigurationProfileEntity
            {
                Id = "PROFILE_TABLE_PRODUCT",
                MaterialNumber = material.MaterialNumber,
                Name = "Table-based Product Configuration"
            };

            // Add procedures to profile (not to net)
            profile.Procedures.Add(new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = proc1.Id,
                SequenceNumber = 1
            });
            profile.Procedures.Add(new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = proc2.Id,
                SequenceNumber = 2
            });

            // Create dependency net with constraints only
            var dependencyNet = new SapLoVcSharp.Data.Models.DependencyNetEntity
            {
                Id = "NET_TABLE_CONFIG",
                Name = "Table-based Configuration Net",
                Description = "Net with 5 constraints using TABLE calls",
                ConfigurationProfileId = profile.Id
            };

            // Add constraints (in dependency order)
            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr1.Id,
                SequenceNumber = 1
            });
            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr2.Id,
                SequenceNumber = 2
            });
            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr3.Id,
                SequenceNumber = 3
            });
            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr4.Id,
                SequenceNumber = 4
            });
            dependencyNet.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = dependencyNet.Id,
                DependencyId = constr5.Id,
                SequenceNumber = 5
            });

            profile.DependencyNets.Add(dependencyNet);
            context.ConfigurationProfiles.Add(profile);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Helper method to create a variant table with columns and rows
        /// </summary>
        /// <summary>
        /// Helper to create variant tables using VariantTableManager.
        /// Creates both metadata and physical SQL table.
        /// </summary>
        private static async Task CreateVariantTableAsync(
            SapLoVcSharp.Data.Sqlite.SqliteDbContext context,
            string tableName,
            string description,
            string[] columnNames,
            bool[] isKeyColumns,
            string[][] rowData)
        {
            // Create SQL data types (all VARCHAR for simplicity)
            var sqlDataTypes = columnNames.Select(_ => "VARCHAR(255)").ToArray();

            // Use VariantTableManager to create table
            var manager = new SapLoVcSharp.Data.Sqlite.Services.VariantTableManager(context);
            await manager.CreateTableAsync(
                tableName,
                description,
                columnNames,
                sqlDataTypes,
                isKeyColumns,
                rowData);
        }

        [Fact]
        public async Task ExecuteConfiguration_WithTableCallsInRestrictionMode_ShouldRestrictAllowedValues()
        {
            // This test verifies SAP LO-VC restriction mode where TABLE calls use ALL characteristics
            // bidirectionally (both as inputs and outputs) to restrict allowed values
            await SeedRestrictionModeConfigurationAsync(_context);
            var executionService = new ExecutionService(_context);

            var result = await executionService.ExecuteConfigurationAsync("BIKE-RESTRICT-001");

            // Output debug information
            Console.WriteLine($"\n=== Restriction Mode Test Results ===");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Cycles Executed: {result.CyclesExecuted}");
            Console.WriteLine($"\nFinal Values:");
            foreach (var kvp in result.FinalValues.OrderBy(k => k.Key))
            {
                Console.WriteLine($"  {kvp.Key} = {GetStringValue(kvp.Value)}");
            }

            // Assert: Configuration should execute successfully
            result.Success.Should().BeTrue("configuration should execute successfully");

            // Assert: FRAME_HEIGHT should be auto-set since only one value matches
            result.FinalValues.Should().ContainKey("FRAME_HEIGHT");
            GetStringValue(result.FinalValues["FRAME_HEIGHT"]).Should().Be("48",
                "only frame height 48 is valid for Trekking/Women combination");

            // Assert: Should stabilize in 2 cycles
            result.CyclesExecuted.Should().BeLessThanOrEqualTo(2,
                "should stabilize quickly with restriction mode");
        }

        private async Task SeedRestrictionModeConfigurationAsync(SapLoVcSharp.Data.Sqlite.SqliteDbContext context)
        {
            var serializer = new SapLoVcSharp.Data.Serialization.AstSerializer();

            // Create class with characteristics
            var bikeClass = new SapLoVcSharp.Data.Models.ClassEntity
            {
                Name = "BIKE_RESTRICT",
                ClassType = "300",
                Description = "Bike for restriction mode testing"
            };

            // MODEL characteristic
            var modelChar = new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "BIKE_RESTRICT",
                Name = "MODEL",
                Description = "Bike Model",
                DataType = "CHAR",
                Length = 20,
                IsRestrictable = true
            };
            modelChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "MODEL",
                Value = "Trekking"
            });
            modelChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "MODEL",
                Value = "Mountain"
            });

            // TYPE characteristic
            var typeChar = new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "BIKE_RESTRICT",
                Name = "TYPE",
                Description = "Bike Type",
                DataType = "CHAR",
                Length = 20,
                IsRestrictable = true
            };
            typeChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "TYPE",
                Value = "Women"
            });
            typeChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "TYPE",
                Value = "Men"
            });

            // FRAME_HEIGHT characteristic (restrictable)
            var frameHeightChar = new SapLoVcSharp.Data.Models.CharacteristicEntity
            {
                ClassName = "BIKE_RESTRICT",
                Name = "FRAME_HEIGHT",
                Description = "Frame Height",
                DataType = "NUM",
                Length = 3,
                IsRestrictable = true  // MUST be restrictable for restriction mode
            };
            frameHeightChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "FRAME_HEIGHT",
                Value = "46"
            });
            frameHeightChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "FRAME_HEIGHT",
                Value = "48"
            });
            frameHeightChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "FRAME_HEIGHT",
                Value = "50"
            });
            frameHeightChar.Values.Add(new SapLoVcSharp.Data.Models.CharacteristicValueEntity
            {
                ClassName = "BIKE_RESTRICT",
                CharacteristicName = "FRAME_HEIGHT",
                Value = "53"
            });

            bikeClass.Characteristics.Add(modelChar);
            bikeClass.Characteristics.Add(typeChar);
            bikeClass.Characteristics.Add(frameHeightChar);
            context.Classes.Add(bikeClass);

            // Create variant table with valid combinations (matching SAP documentation example)
            await CreateVariantTableAsync(
                context,
                "T_FRAME_HEIGHT",
                "Valid frame height combinations",
                new[] { "MODEL", "TYPE", "FRAME_HEIGHT" },
                new[] { false, false, false },  // No key fields defined (restriction mode doesn't need them)
                new[]
                {
                    new[] { "Trekking", "Women", "48" },  // Only one valid value for Trekking/Women
                    new[] { "Trekking", "Men", "48" },
                    new[] { "Trekking", "Men", "53" },
                    new[] { "Mountain", "Women", "46" },
                    new[] { "Mountain", "Women", "50" }
                });

            // Create material
            var bike = new SapLoVcSharp.Data.Models.MaterialEntity
            {
                MaterialNumber = "BIKE-RESTRICT-001",
                Description = "Bike for restriction mode testing"
            };
            bike.ClassAssignments.Add(new SapLoVcSharp.Data.Models.MaterialClassAssignmentEntity
            {
                MaterialNumber = "BIKE-RESTRICT-001",
                ClassName = "BIKE_RESTRICT"
            });
            context.Materials.Add(bike);

            // Create configuration profile
            var profile = new SapLoVcSharp.Data.Models.ConfigurationProfileEntity
            {
                Id = "PROFILE_BIKE_RESTRICT",
                Name = "Bike Restriction Mode Profile",
                MaterialNumber = "BIKE-RESTRICT-001"
            };

            // Create procedure to set initial values
            var procSource = @"
                $SELF.MODEL = 'Trekking',
                $SELF.TYPE = 'Women'";

            var proc = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "PROC_SET_BIKE_TYPE",
                Name = "SetBikeType",
                Type = "Procedure",
                SourceCode = procSource,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ProcedureParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(procSource).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(proc);

            profile.Procedures.Add(new SapLoVcSharp.Data.Models.ConfigurationProfileProcedureEntity
            {
                ConfigurationProfileId = profile.Id,
                DependencyId = proc.Id,
                SequenceNumber = 1
            });

            // Create constraint using RESTRICTION MODE (no ?= operators)
            // This matches the SAP documentation example from line 2651
            var constraintSource = @"
                OBJECTS:
                    BIKE IS_A (300) BIKE_RESTRICT WHERE
                        lv_MODEL = MODEL;
                        lv_TYPE = TYPE;
                        lv_FRAME_HEIGHT = FRAME_HEIGHT

                RESTRICTIONS:
                    TABLE T_FRAME_HEIGHT (
                        MODEL = lv_MODEL,
                        TYPE = lv_TYPE,
                        FRAME_HEIGHT = lv_FRAME_HEIGHT
                    )

                INFERENCES:
                    lv_FRAME_HEIGHT";

            var constraint = new SapLoVcSharp.Data.Models.DependencyEntity
            {
                Id = "CONSTR_RESTRICT_FRAME",
                Name = "RestrictFrameHeight",
                Type = "Constraint",
                SourceCode = constraintSource,
                Status = "Active",
                AstJson = serializer.Serialize(
                    new SapLoVcSharp.Core.Parsing.DependencyParsers.ConstraintParser(
                        new SapLoVcSharp.Core.Lexing.Lexer(constraintSource).Tokenize()
                    ).Parse()
                )
            };
            context.Dependencies.Add(constraint);

            // Create dependency net
            var net = new SapLoVcSharp.Data.Models.DependencyNetEntity
            {
                Id = "NET_BIKE_RESTRICT",
                Name = "BikeRestrictionNet",
                ConfigurationProfileId = profile.Id
            };
            net.Constraints.Add(new SapLoVcSharp.Data.Models.DependencyNetConstraintEntity
            {
                DependencyNetId = net.Id,
                DependencyId = constraint.Id,
                SequenceNumber = 1
            });

            profile.DependencyNets.Add(net);
            bike.ConfigurationProfile = profile;

            await context.SaveChangesAsync();
        }
    }
}
