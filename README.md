# MDD Booster

A code generator that supports MDD.

**Install**

> `dotnet tool install --global MDD-Booster`

**Update**

> `dotnet tool update mdd-booster --global --no-cache`

**Run**

> `mdd <directory-path>` // `directory-path` Run with parameters
> `mdd` // Current Path

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
}
```

**M3L**
`tables.m3l`

Samples .1
```

## IEntity

## IKeyEntity : IEntity
- _key: guid			            [PK, Without]

## IAtEntity : IEntity
- CreatedAt: datetime = "@now"		[Insert("@now")]
- CreatedBy: string = "@by"			[Insert("@by")]
- UpdatedAt?: datetime				[Update("@now")]
- UpdatedBy?: string				[Update("@by")]

## KeyEntity : IKeyEntity, @abstract

## EntityBase : KeyEntity, IAtEntity, @default, @abstract

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