# MDD Booster

A code generator that supports MDD.

**Install**

> `dotnet tool install --global MDD-Booster`

**Update**

> `dotnet tool update mdd-booster --global --no-cache`

**Run**

> `mdd <directory-path>` // `directory-path` Run with parameters
<br /> `mdd` // Current Path

**Settings**

`settings.json`

```
{
	"modelProject": {
		"path": "../src/MyApp",
		"ns": "MyApp",
		"usings": [ "Iyu.Entity" ]
	},
	"databaseProject": {
		"path": "../src/MyApp.Database"
	},
	"serverProject": {
		"path": "../src/MyApp.MainServer",
		"ns": "MyApp.MainServer",
		"useGraphQL": true
	},
	"webFrontEnd": {
		"models": [
			{
				"ns": "MetaModels",
				"modelPath": "../src/MyApp.Bridge/Models",
				"ts-file": "../src/MyApp.MainServer.WebApp.FE/src/models/meta-models.ts"
			}
		]
	},	
	"flutterProject": {
		"models": [
			{ 
				"cs-file": "../src/Plands.MainServer/Contracts/PlanRequests.cs",
				"dart-file": "../src/plands_app/lib/src/contracts/plan_requests.dart"
		 	}
		]
	},
}
```

## M3L

`tables.m3l`

### Interface

prefix: `I`

```
## IEntity
```

### Abstract

options: `@abstract`

```
## GuidEntity : IGuidEntity, @abstract
```

### Implementation

```
## IGuidEntity : IEntity
- _id: guid			            [PK, Without]
```

### Default Entity

options: `@default`

### Property (Column)

Start with a list item ('-').

`- {name}: {type}`

default value: `= {default}`

options: `[options]`

description: `// DESCRIPTION`

label: `- {name}({label}): {type}`

#### nullable

Add a `?` after the Name or Type.

```
- Locale?: string
- Location_id: guid? // DESCRIPTION
```

#### Foriegn Key

Once the naming convention is applied, you can also write it simply. [FK]

`- ServiceAccount_id: guid [FK]`

If you don't follow naming conventions

`- FollowerId: guid [FK: Account._id]`

On Delete or Update Actions

`[FK: {Table}.{Column}, ON {DELETE|UPDATE} {Action}]]`

`- Account_id: guid [FK: Account._id, ON DELETE NO ACTION]`

#### Unique

`- Email: string(256) [UQ]`

If you combine them, add an item for each.

`- @unique: ({columns})`

```
## PlanTag
- Plan_id: guid
- Tag_id: guid
- @unique: (Plan_id, Tag_id)
```

#### Index

use index `[UI]`

```
- EntityName: string [UI]
```

#### Anyting attributes

You can add anything by wrapping it in '[]'. [anything] 

`- PasswordHash?: string(256)		[DataType(DataType.Password)][JsonIgnore]`

## Samples

### sample #1

```

## IEntity

## IGuidEntity : IEntity
- _id: guid			            [PK, Without]

## IAtEntity : IEntity
- CreatedAt: datetime = "@now"		[Insert("@now")]
- CreatedBy: string = "@by"			[Insert("@by")]
- UpdatedAt?: datetime				[Update("@now")]
- UpdatedBy?: string				[Update("@by")]

## GuidEntity : IGuidEntity, @abstract

## EntityBase : GuidEntity, IAtEntity, @default, @abstract

## Account:
- Email: string(256)
- NormalizedEmail: string(256)		[UQ]
- EmailConfirmed?: bool
- Name: string
- Phone?: string
- PhoneConfirmed?: bool
- PasswordHash?: string(256)		[DataType(DataType.Password)]
- PasswordChangedAt?: datetime
- AcceptTermsAt?: datetime

```