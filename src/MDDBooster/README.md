# MDDBooster

MDDBooster is a tool that allows you to easily design database schemas and model classes using Meta Model Markup Language (M3L) and generates them automatically.

## Table of Contents

- [Introduction](#introduction)
- [Installation](#installation)
- [Basic Concepts](#basic-concepts)
- [Syntax Guide](#syntax-guide)
  - [File Definition](#file-definition)
  - [Entity Definition](#entity-definition)
  - [Interface Definition](#interface-definition)
  - [Inheritance and Implementation](#inheritance-and-implementation)
  - [Property (Column) Definition](#property-column-definition)
  - [Index Definition](#index-definition)
  - [Foreign Key Definition](#foreign-key-definition)
  - [Unique Constraints](#unique-constraints)
  - [Default Value Definition](#default-value-definition)
  - [Adding Comments](#adding-comments)
  - [Enum Definition](#enum-definition)
- [Advanced Features](#advanced-features)
  - [Abstract Classes](#abstract-classes)
  - [Default Classes](#default-classes)
  - [Composite Indexes](#composite-indexes)
  - [Composite Unique Constraints](#composite-unique-constraints)
- [Complete Example](#complete-example)
- [Generated Output](#generated-output)
- [Configuration File](#configuration-file)
- [FAQ](#faq)

## Introduction

M3L (Meta Model Markup Language) is a markdown-based specification language that allows you to concisely define database table structures and model classes. MDDBooster analyzes these M3L files to automatically generate the following outputs:

- SQL scripts (MS SQL Server, PostgreSQL supported)
- C# model classes
- TypeScript interfaces
- Flutter/Dart model classes
- GraphQL schemas

### Key Benefits

- **Conciseness**: Markdown-based syntax that is easy to read and write.
- **Database Independence**: Define models without being constrained by specific database syntax.
- **Code Generation Consistency**: Generate consistent code for multiple platforms with a single definition.
- **Rapid Development**: Minimize repetitive code writing tasks to increase development speed.

> M3L was inspired by [DBML](https://dbml.dbdiagram.io/docs/) and [Markdown](https://www.markdownguide.org/cheat-sheet/).

## Installation

MDDBooster is provided as a .NET tool:

```bash
# Install as a .NET tool
dotnet tool install --global MDDBooster

# Verify installation
mddbooster --version
```

## Basic Concepts

MDDBooster uses the following basic concepts:

- **Entity**: Represents a database table or model class.
- **Property**: Represents a column or field of an entity.
- **Interface**: Represents a set of properties that an entity can implement.
- **Attribute**: Represents additional information that can be assigned to properties or entities.

## Syntax Guide

### File Definition

M3L files have `.mdd` or `.m3l` extensions.

```markdown
# Project Name
```

### Entity Definition

Entities (tables) start with two hash marks (`##`), and names and labels can be defined in various ways:

```markdown
## User
```

Adding a label (display name):

```markdown
## User (User)
```

Or adding a label with attributes:

```markdown
## User [label: User]
```

### Interface Definition

Interfaces have uppercase names starting with 'I':

```markdown
## IEntity
- Id: int [PK]
```

### Inheritance and Implementation

List inherited classes and implemented interfaces after a colon (`:`):

```markdown
## IKeyEntity : IEntity
- _key: guid [PK]

## EntityBase : KeyEntity, IAtEntity, @abstract
```

### Property (Column) Definition

Properties start with a dash (`-`) and can be defined in various ways:

**Basic Syntax**:
```markdown
- Name: string
```

**Size Specification**:
```markdown
- Name: string(50)     // 50 character limit
- Description: string(max)  // Maximum size
```

**Required/Nullable Specification**:
```markdown
- Name: string         // NOT NULL (required)
- Name?: string        // NULL allowed (Nullable)
- Email: string?       // NULL allowed (alternative syntax)
```

**Adding Labels**:
```markdown
- Name(Name): string   // Label: 'Name'
- Age(Age,Years): int  // Label: 'Age', short name: 'Years'
```

**Primary Key Specification**:
```markdown
- Id: int [PK]         // Primary key
- _id: guid [PK]       // GUID primary key
```

### Index Definition

Indexes can be defined in several ways:

**Single Column Index**:
```markdown
- Email: string [UI]   // Create index
- Phone: string [IX]   // UI and IX are the same (alternative syntax)
```

**Index Name Specification**:
```markdown
- Email: string [IX_CustomEmailIndex]
```

### Foreign Key Definition

Foreign keys can be defined in various ways:

**Basic Syntax**:
```markdown
- Customer_id: guid [FK]  // Infer FK from column name (References Customer table)
```

**Explicit Reference**:
```markdown
- WriterId: guid [FK: User]        // Reference User table's PK
- AuthorId: guid [FK: User._id]    // Reference User table's _id column
```

**Delete/Update Action Specification**:
```markdown
- Department_id: guid [FK: Department, OnDelete(Cascade)]  // Cascade on delete
- Manager_id: guid [FK: User, OnDelete(SetNull), OnUpdate(NoAction)]  // Set NULL on delete
```

### Unique Constraints

Unique constraints can be applied to single columns or composite columns:

**Single Column**:
```markdown
- Email: string [UQ]   // Unique constraint
- Code: string [UK]    // UQ and UK are the same (alternative syntax)
```

**Composite Columns**:
```markdown
- @unique: (FirstName, LastName)  // Composite unique constraint
```

### Default Value Definition

Default values are specified after an equals sign (`=`):

```markdown
- CreatedAt: datetime = "@now"    // Current time
- IsActive: bool = true           // Boolean value
- Status: string = "pending"      // String
- Count: int = 0                  // Number
```

Special default values:
```markdown
- CreatedAt: datetime = "@now"    // Current time
- CreatedBy: string = "@by"       // Current user
```

### Adding Comments

Comments can be added in various ways:

**Comment Syntax**:
```markdown
- Name: string         // This is a comment
- Age: int             # This is also a comment
- Description: string  /* This is also a comment */
```

**Description Attribute**:
```markdown
- Name: string [desc: User's name]
```

### Enum Definition

Enum types are defined as follows:

```markdown
- Status: enum(Pending|Active|Suspended|Deleted)
```

Name specification and key values:
```markdown
- Role: enum(key: Admin|User|Guest, name: UserRoles)
```

Value specification:
```markdown
- Priority: enum(Low=0|Medium=5|High=10, name: TaskPriority)
```

## Advanced Features

### Abstract Classes

Abstract classes are specified with the `@abstract` keyword:

```markdown
## EntityBase : KeyEntity, IAtEntity, @abstract
```

### Default Classes

Default classes are specified with the `@default` keyword:

```markdown
## EntityBase : KeyEntity, IAtEntity, @default, @abstract
```

### Composite Indexes

Composite indexes are defined with the `@index` directive:

```markdown
## User
- FirstName: string
- LastName: string
- @index: [FirstName, LastName]  // Name index
```

Name specification:
```markdown
## User
- FirstName: string
- LastName: string
- @index: [FirstName, LastName] [IX_FullName]  // Name specification
```

### Composite Unique Constraints

Composite unique constraints are defined with the `@unique` directive:

```markdown
## Enrollment
- StudentId: guid
- CourseId: guid
- @unique: (StudentId, CourseId)  // A student can only register for one course
```

## Complete Example

```markdown
## IEntity

## IKeyEntity : IEntity
- _key: guid [PK, Without]

## IAtEntity : IEntity
- CreatedAt: datetime = "@now" [Insert("@now")]
- CreatedBy: string = "@by" [Insert("@by")]
- UpdatedAt?: datetime [Update("@now")]
- UpdatedBy?: string [Update("@by")]

## ISoftDeletable : IEntity
- DeletedAt?: datetime [Update("@now")]
- DeletedBy?: string [Update("@by")]

## KeyEntity : IKeyEntity, @abstract

## EntityBase : KeyEntity, IAtEntity, @default, @abstract

## User : EntityBase, ISoftDeletable
- Email: string(256) [UQ]
- EmailConfirmed: bool = false
- Name(Name): string
- PasswordHash?: string(256) [JsonIgnore]
- Phone?: string
- IsActive: bool = true
- Role: enum(Admin|User|Guest, name: UserRoles)
- @index: [Email, IsActive] [IX_ActiveEmail]

## Product : EntityBase, ISoftDeletable
- Name(Product Name): string
- Code: string(20) [UQ]
- Price: decimal(10,2)
- Category_id: guid [FK: ProductCategory]
- InStock: int = 0
- @unique: (Name, Category_id)
```

## Generated Output

MDDBooster generates the following outputs from the above example:

1. SQL Scripts (MS SQL Server):
   - User.sql
   - Product.sql
   - Related indexes and foreign keys

2. C# Model Classes:
   - IEntity.cs
   - IKeyEntity.cs
   - IAtEntity.cs
   - ISoftDeletable.cs
   - KeyEntity.cs
   - EntityBase.cs
   - User.cs
   - Product.cs

3. TypeScript Interfaces (optional):
   - models.ts (includes all interfaces)

4. Dart Model Classes (optional):
   - user.dart
   - product.dart

## Configuration File

You can configure generation options with a `mddbooster.json` file:

```json
{
  "basePath": "./",
  "modelProject": {
    "path": "MyProject.Models",
    "ns": "MyProject.Models",
    "usings": ["System.Text.Json"]
  },
  "databaseProject": {
    "path": "MyProject.Database",
    "kind": "MSSQL"
  },
  "serverProject": {
    "path": "MyProject.Server",
    "ns": "MyProject.Server"
  },
  "webFrontEnd": {
    "models": [
      {
        "ns": "models",
        "modelPath": "MyProject.Models/Entity",
        "tsFile": "ClientApp/src/models/index.ts"
      }
    ]
  },
  "flutterProject": {
    "output": "mobile/lib/models",
    "models": [
      {
        "csFile": "MyProject.Models/Entity/User.cs",
        "dartFile": "mobile/lib/models/user.dart"
      }
    ]
  }
}
```

## FAQ

### Q: What data types are supported?
A: The main data types are as follows:
- `string`: String (size can be specified)
- `int`: Integer
- `long`: Large integer
- `decimal`: Decimal number (size can be specified, e.g., decimal(10,2))
- `float`: Floating-point number
- `double`: Floating-point number (larger range)
- `bool`: Boolean value
- `datetime`: Date and time
- `guid`: GUID/UUID
- `enum`: Enumeration (list of values can be specified)

### Q: What are the foreign key delete action options?
A: The main options are as follows:
- `Cascade`: Delete connected records together
- `SetNull`: Set references in connected records to NULL
- `NoAction`: No action (may cause errors)

### Q: How are the special default values `@now` and `@by` converted?
A: They are converted differently depending on the database:
- MS SQL: `@now` ¡æ `GETDATE()`, `@by` ¡æ `'@system'`
- PostgreSQL: `@now` ¡æ `NOW()`, `@by` ¡æ `'@system'`

### Q: Can I use multiple M3L files?
A: Yes, you can use multiple M3L files. MDDBooster processes all `.mdd` and `.m3l` files in the specified path.