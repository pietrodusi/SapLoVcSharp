# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SapLoVcSharp is a C# implementation of SAP's LO-VC (Logistics - Variant Configuration) dependency language. It provides a complete parser, AST (Abstract Syntax Tree), and execution engine for modeling and executing product configuration rules and constraints.

**Target Framework**: .NET 10.0

## Building and Testing

### Build the Solution
```bash
dotnet build
```

### Run All Tests
```bash
dotnet test
```

### Run Tests for Specific Project
```bash
dotnet test SapLoVcSharp.Core.Tests
dotnet test SapLoVcSharp.Data.Tests
```

### Run a Single Test
```bash
# Run specific test by fully qualified name
dotnet test --filter "FullyQualifiedName~LexerTests.Tokenize_SimpleConstraint"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~LexerTests"
```

### Database Migrations (SQLite)
```bash
# Add a new migration
dotnet ef migrations add MigrationName --project SapLoVcSharp.Data.Sqlite

# Update database
dotnet ef database update --project SapLoVcSharp.Data.Sqlite
```

## Architecture

### Project Structure

The solution follows a layered architecture with clear separation of concerns:

```
SapLoVcSharp.Core/              # Core parsing and AST logic (no dependencies)
├── Lexing/                     # Tokenization of SAP dependency language
├── Parsing/                    # Parser and dependency-specific parsers
│   └── DependencyParsers/      # ConstraintParser, ProcedureParser, etc.
└── Ast/                        # AST node definitions
    ├── Dependencies/           # ConstraintNode, ProcedureNode, etc.
    ├── Expressions/            # BinaryExpressionNode, MemberAccessNode, etc.
    └── Statements/             # AssignmentNode, TableCallNode, etc.

SapLoVcSharp.Data/              # Data layer abstractions
├── Models/                     # Domain entities (MaterialEntity, CharacteristicEntity, etc.)
└── Serialization/              # AST JSON serialization

SapLoVcSharp.Data.Sqlite/      # EF Core + SQLite implementation
├── SqliteDbContext.cs          # DbContext with entity configuration
└── Migrations/                 # EF Core migrations

SapLoVcSharp.Core.Tests/        # Unit tests for parsing/lexing
SapLoVcSharp.Data.Tests/        # Integration tests for data layer
Test/                           # Console app for ad-hoc testing
```

### Key Domain Concepts

**Configuration Structure:**
- **Material**: Configurable product (e.g., "BIKE-001")
- **ConfigurationProfile**: Links materials to their configuration rules
- **Class**: Organizes characteristics into logical groups (e.g., "BIKE_CLASS")
- **Characteristic**: Variables/properties (e.g., "COLOR", "MODEL", "FRAME_HEIGHT")
- **CharacteristicValue**: Allowed values for characteristics

**Dependency Types** (4 types of business rules):
1. **Procedure**: Imperative statements that execute sequentially
   - Example: `$SELF.COLOR = 'Red' IF MODEL = 'Racing'`
2. **Constraint**: Declarative rules with OBJECTS/CONDITION/RESTRICTIONS/INFERENCES sections
3. **Precondition**: Boolean expressions that gate availability
4. **SelectionCondition**: Controls which values are available for selection

**Variant Tables**: Tabular representation of valid characteristic combinations, queryable via `TABLE` calls in dependencies.

### AST and Parsing

The parser converts SAP dependency source code into a strongly-typed AST:

```
Source Code → Lexer → Tokens → Parser → AST (DependencyNode)
```

**Key AST Patterns:**
- All AST nodes implement the **Visitor Pattern** via `IAstVisitor`
- Separate hierarchies for expressions vs. statements
- `MemberAccessNode` for procedures (`$SELF.COLOR`) vs. `ConstraintMemberAccessNode` for constraints (`PC.COLOR`)

**Special Language Features:**
- Object references: `$SELF`, `$PARENT`, `$ROOT`
- Built-in functions: Mathematical (SIN, COS, etc.), string (LC, UC), configuration (`$SET_DEFAULT`, `$SUM_PARTS`)
- Table calls: `TABLE T_NAME (COL1 = value1, COL2 ?= optional)` (async database lookup)
- Context-aware dot parsing: Dots can be section terminators OR member access operators

### Data Layer Architecture

**Critical Design Decisions:**
1. **Composite Keys**: Characteristics use natural keys `(ClassName, Name)` to match SAP's structure
2. **AST Serialization**: Dependencies are stored as JSON in the database (using `AstSerializer`)
3. **Async Operations**: All execution is async to support database-backed variant table resolution
4. **Cascade Deletes**: Proper referential integrity (e.g., deleting a Material cascades to Profiles and Nets)
5. **Audit Trail**: `DependencyExecutionLog` tracks every execution with context and results

**Entity Relationships:**
- Many-to-Many: Materials ↔ Classes, Dependencies ↔ Nets, Values ↔ Dependencies
- Ordered Collections: Dependencies in nets have sequence numbers for deterministic execution

### Recent Architectural Changes

The system recently underwent major refactoring (see commits 92370a3, 2e2b490):
- **Async database-backed table resolution**: Moved from in-memory to database-backed variant tables
- **Constraint execution refactoring**: Simplified with `ShouldExecuteWithContext` and `ApplyInferencesAsync`
- **Built-in function support**: Added 5+ built-in functions for procedures

All constraint/procedure execution operations are now **async** to support database I/O.

## Development Guidelines

### Adding New Dependency Types
1. Create node class in `SapLoVcSharp.Core/Ast/Dependencies/`
2. Add parser in `SapLoVcSharp.Core/Parsing/DependencyParsers/`
3. Update `AstSerializer` in `SapLoVcSharp.Data/Serialization/` with polymorphic type
4. Add visitor method to `IAstVisitor`
5. Write comprehensive tests in `SapLoVcSharp.Core.Tests/`

### Adding New Expressions/Statements
Same pattern as above, but in respective `Ast/Expressions/` or `Ast/Statements/` directories.

### Extending Database Model
1. Add entity to `SapLoVcSharp.Data/Models/`
2. Configure in `SqliteDbContext.OnModelCreating()`
3. Create EF migration: `dotnet ef migrations add MigrationName`
4. Add tests in `SapLoVcSharp.Data.Tests/`

### Testing Patterns
- **Core Tests**: Use `LexerTestHelper` and `ParserTestHelper` for concise test setup
- **Data Tests**: Use in-memory SQLite via `SqliteInMemoryDbContextFactory`
- **Assertions**: FluentAssertions for readable test assertions
- Real-world SAP examples are preferred over synthetic test cases

### SAP Language Syntax Notes
- Comments start with `*` (entire line)
- Sections can end with `.` or implicitly when next section starts
- Keywords are case-insensitive
- Negative numbers require context-aware parsing (e.g., `5 - 3` vs. `x = -5`)
- `?=` in table calls denotes optional parameters

## Technology Stack
- **.NET 10.0**: Latest C# language features enabled
- **Entity Framework Core 10.0**: ORM with SQLite provider
- **System.Text.Json**: Modern JSON serialization with polymorphic support
- **xUnit 2.9**: Test framework with FluentAssertions
- **SQLite**: Lightweight, portable database (easily swappable for SQL Server/PostgreSQL)
