using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Data.Models;

namespace SapLoVcSharp.Data.Sqlite
{
    public class SqliteDbContext : DbContext
    {
        // Existing - Dependencies
        public DbSet<DependencyEntity> Dependencies { get; set; } = null!;
        public DbSet<DependencyExecutionLog> ExecutionLogs { get; set; } = null!;

        // NEW - Domain Model
        public DbSet<MaterialEntity> Materials { get; set; } = null!;
        public DbSet<ConfigurationProfileEntity> ConfigurationProfiles { get; set; } = null!;
        public DbSet<DependencyNetEntity> DependencyNets { get; set; } = null!;
        public DbSet<ClassEntity> Classes { get; set; } = null!;
        public DbSet<CharacteristicEntity> Characteristics { get; set; } = null!;
        public DbSet<CharacteristicValueEntity> CharacteristicValues { get; set; } = null!;

        // Junction tables
        public DbSet<MaterialClassAssignmentEntity> MaterialClassAssignments { get; set; } = null!;
        public DbSet<DependencyNetConstraintEntity> DependencyNetConstraints { get; set; } = null!;
        public DbSet<ConfigurationProfileProcedureEntity> ConfigurationProfileProcedures { get; set; } = null!;
        public DbSet<ValueDependencyEntity> ValueDependencies { get; set; } = null!;

        // Variant Tables (metadata only - actual data is in dedicated tables)
        public DbSet<VariantTableEntity> VariantTables { get; set; } = null!;
        public DbSet<VariantTableColumnEntity> VariantTableColumns { get; set; } = null!;

        // Product Matrices
        public DbSet<ProductMatrixEntity> ProductMatrices { get; set; } = null!;
        public DbSet<ProductMatrixSheetEntity> ProductMatrixSheets { get; set; } = null!;
        public DbSet<ProductMatrixCharacteristicEntity> ProductMatrixCharacteristics { get; set; } = null!;
        public DbSet<ProductMatrixRowEntity> ProductMatrixRows { get; set; } = null!;
        public DbSet<ProductMatrixCellEntity> ProductMatrixCells { get; set; } = null!;

        public SqliteDbContext(DbContextOptions<SqliteDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureDependencies(modelBuilder);
            ConfigureMaterials(modelBuilder);
            ConfigureClasses(modelBuilder);
            ConfigureCharacteristics(modelBuilder);
            ConfigureRelationships(modelBuilder);
            ConfigureVariantTables(modelBuilder);
            ConfigureProductMatrices(modelBuilder);
        }

        private void ConfigureDependencies(ModelBuilder modelBuilder)
        {
            // Configure DependencyEntity
            modelBuilder.Entity<DependencyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SourceCode).IsRequired();
                entity.Property(e => e.AstJson).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Version).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Name);
            });

            // Configure DependencyExecutionLog
            modelBuilder.Entity<DependencyExecutionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DependencyId).IsRequired();
                entity.Property(e => e.ExecutedAt).IsRequired();
                entity.Property(e => e.ContextJson).IsRequired();
                entity.Property(e => e.ResultJson).IsRequired();
                entity.Property(e => e.DurationMs).IsRequired();
                entity.Property(e => e.Success).IsRequired();
                entity.Property(e => e.IsConsistent).IsRequired();

                entity.HasIndex(e => e.DependencyId);
                entity.HasIndex(e => e.ExecutedAt);
            });
        }

        private void ConfigureMaterials(ModelBuilder modelBuilder)
        {
            // Material
            modelBuilder.Entity<MaterialEntity>(entity =>
            {
                entity.HasKey(e => e.MaterialNumber);
                entity.Property(e => e.MaterialNumber).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MaterialType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.BaseUnit).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.MaterialType);
            });

            // Configuration Profile
            modelBuilder.Entity<ConfigurationProfileEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MaterialNumber).IsRequired().HasMaxLength(40);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Material)
                    .WithOne(m => m.ConfigurationProfile)
                    .HasForeignKey<ConfigurationProfileEntity>(e => e.MaterialNumber)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.MaterialNumber);
            });

            // Dependency Net
            modelBuilder.Entity<DependencyNetEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ConfigurationProfileId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.ConfigurationProfile)
                    .WithMany(p => p.DependencyNets)
                    .HasForeignKey(e => e.ConfigurationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ConfigurationProfileId);
                entity.HasIndex(e => e.Status);
            });
        }

        private void ConfigureClasses(ModelBuilder modelBuilder)
        {
            // Class - Name is PK
            modelBuilder.Entity<ClassEntity>(entity =>
            {
                entity.HasKey(e => e.Name);  // User-defined PK (e.g., "BIKE_CLASS")
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ClassType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => e.ClassType);
                entity.HasIndex(e => e.Status);
            });
        }

        private void ConfigureCharacteristics(ModelBuilder modelBuilder)
        {
            // Characteristic - Composite Key (ClassName + Name)
            modelBuilder.Entity<CharacteristicEntity>(entity =>
            {
                entity.HasKey(e => new { e.ClassName, e.Name });  // Composite PK

                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.DataType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Length).IsRequired();
                entity.Property(e => e.DecimalPlaces).IsRequired();
                entity.Property(e => e.IsMultiValue).IsRequired();
                entity.Property(e => e.IsRestrictable).IsRequired();
                entity.Property(e => e.IsRequired).IsRequired();
                entity.Property(e => e.IsDisplayOnly).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Characteristics)
                    .HasForeignKey(e => e.ClassName)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Name);
            });

            // Characteristic Value - Composite Key (ClassName + CharacteristicName + Value)
            modelBuilder.Entity<CharacteristicValueEntity>(entity =>
            {
                entity.HasKey(e => new { e.ClassName, e.CharacteristicName, e.Value });

                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CharacteristicName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsDefault).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Characteristic)
                    .WithMany(c => c.Values)
                    .HasForeignKey(e => new { e.ClassName, e.CharacteristicName })
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Value);
            });
        }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Material-Class (Many-to-Many)
            modelBuilder.Entity<MaterialClassAssignmentEntity>(entity =>
            {
                entity.HasKey(e => new { e.MaterialNumber, e.ClassName });
                entity.Property(e => e.MaterialNumber).IsRequired().HasMaxLength(40);
                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AssignedAt).IsRequired();

                entity.HasOne(e => e.Material)
                    .WithMany(m => m.ClassAssignments)
                    .HasForeignKey(e => e.MaterialNumber)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Class)
                    .WithMany(c => c.MaterialAssignments)
                    .HasForeignKey(e => e.ClassName)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DependencyNet-Constraint (Many-to-Many)
            modelBuilder.Entity<DependencyNetConstraintEntity>(entity =>
            {
                entity.HasKey(e => new { e.DependencyNetId, e.DependencyId });
                entity.Property(e => e.DependencyNetId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DependencyId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SequenceNumber).IsRequired();
                entity.Property(e => e.AssignedAt).IsRequired();

                entity.HasOne(e => e.DependencyNet)
                    .WithMany(n => n.Constraints)
                    .HasForeignKey(e => e.DependencyNetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Dependency)
                    .WithMany()
                    .HasForeignKey(e => e.DependencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DependencyNetId);
                entity.HasIndex(e => e.SequenceNumber);
            });

            // ConfigurationProfile-Procedure (Many-to-Many)
            modelBuilder.Entity<ConfigurationProfileProcedureEntity>(entity =>
            {
                entity.HasKey(e => new { e.ConfigurationProfileId, e.DependencyId });
                entity.Property(e => e.ConfigurationProfileId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DependencyId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SequenceNumber).IsRequired();
                entity.Property(e => e.AssignedAt).IsRequired();

                entity.HasOne(e => e.ConfigurationProfile)
                    .WithMany(p => p.Procedures)
                    .HasForeignKey(e => e.ConfigurationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Dependency)
                    .WithMany()
                    .HasForeignKey(e => e.DependencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ConfigurationProfileId);
                entity.HasIndex(e => e.SequenceNumber);
            });

            // CharacteristicValue-Dependency (Many-to-Many)
            modelBuilder.Entity<ValueDependencyEntity>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.ClassName,
                    e.CharacteristicName,
                    e.CharacteristicValue,
                    e.DependencyId
                });

                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CharacteristicName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CharacteristicValue).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DependencyId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DependencyType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AssignedAt).IsRequired();

                entity.HasOne(e => e.CharacteristicValueEntity)
                    .WithMany(v => v.Dependencies)
                    .HasForeignKey(e => new { e.ClassName, e.CharacteristicName, e.CharacteristicValue })
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Dependency)
                    .WithMany()
                    .HasForeignKey(e => e.DependencyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureVariantTables(ModelBuilder modelBuilder)
        {
            // Variant Table (metadata only)
            modelBuilder.Entity<VariantTableEntity>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DatabaseTableName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.IsTableCreated).IsRequired();
                entity.Property(e => e.RowCount).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.DatabaseTableName).IsUnique();
            });

            // Variant Table Column (metadata only)
            modelBuilder.Entity<VariantTableColumnEntity>(entity =>
            {
                entity.HasKey(e => new { e.TableName, e.ColumnIndex });

                entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ColumnIndex).IsRequired();
                entity.Property(e => e.CharacteristicName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ClassName).HasMaxLength(100);
                entity.Property(e => e.IsKeyColumn).IsRequired();
                entity.Property(e => e.SqlDataType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Table)
                    .WithMany(t => t.Columns)
                    .HasForeignKey(e => e.TableName)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TableName, e.ColumnIndex });
            });
        }

        private void ConfigureProductMatrices(ModelBuilder modelBuilder)
        {
            // Product Matrix
            modelBuilder.Entity<ProductMatrixEntity>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.MaterialNumber).IsRequired().HasMaxLength(40);
                entity.Property(e => e.GeneratedVariantTableName).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Material)
                    .WithMany()
                    .HasForeignKey(e => e.MaterialNumber)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GeneratedVariantTable)
                    .WithMany()
                    .HasForeignKey(e => e.GeneratedVariantTableName)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.MaterialNumber);
                entity.HasIndex(e => e.Status);
            });

            // Product Matrix Sheet
            modelBuilder.Entity<ProductMatrixSheetEntity>(entity =>
            {
                entity.HasKey(e => new { e.MatrixName, e.SheetIndex });
                entity.Property(e => e.MatrixName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SheetIndex).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(255);

                entity.HasOne(e => e.Matrix)
                    .WithMany(m => m.Sheets)
                    .HasForeignKey(e => e.MatrixName)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product Matrix Characteristic
            modelBuilder.Entity<ProductMatrixCharacteristicEntity>(entity =>
            {
                entity.HasKey(e => new { e.MatrixName, e.SheetIndex, e.CharacteristicIndex });
                entity.Property(e => e.MatrixName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SheetIndex).IsRequired();
                entity.Property(e => e.CharacteristicIndex).IsRequired();
                entity.Property(e => e.CharacteristicName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ClassName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DisplayMode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ResultCharacteristicName).HasMaxLength(100);
                entity.Property(e => e.ResultClassName).HasMaxLength(100);

                entity.HasOne(e => e.Sheet)
                    .WithMany(s => s.Characteristics)
                    .HasForeignKey(e => new { e.MatrixName, e.SheetIndex })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product Matrix Row
            modelBuilder.Entity<ProductMatrixRowEntity>(entity =>
            {
                entity.HasKey(e => new { e.MatrixName, e.SheetIndex, e.RowIndex });
                entity.Property(e => e.MatrixName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SheetIndex).IsRequired();
                entity.Property(e => e.RowIndex).IsRequired();
                entity.Property(e => e.RowLabel).HasMaxLength(255);

                entity.HasOne(e => e.Sheet)
                    .WithMany(s => s.Rows)
                    .HasForeignKey(e => new { e.MatrixName, e.SheetIndex })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product Matrix Cell
            modelBuilder.Entity<ProductMatrixCellEntity>(entity =>
            {
                entity.HasKey(e => new { e.MatrixName, e.SheetIndex, e.RowIndex, e.CharacteristicIndex, e.ValueIndex });
                entity.Property(e => e.MatrixName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SheetIndex).IsRequired();
                entity.Property(e => e.RowIndex).IsRequired();
                entity.Property(e => e.CharacteristicIndex).IsRequired();
                entity.Property(e => e.ValueIndex).IsRequired();
                entity.Property(e => e.Value).HasMaxLength(255);

                entity.HasOne(e => e.Row)
                    .WithMany(r => r.Cells)
                    .HasForeignKey(e => new { e.MatrixName, e.SheetIndex, e.RowIndex })
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}