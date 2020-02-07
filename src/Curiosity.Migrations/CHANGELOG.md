# Changelog

## [1.0] - 2019-11-18

Moved to [SIIS Ltd](https://github.com/SIIS-Ltd/Migrations).

## [0.4] - 2019-10-31

### Added 

- [#13](https://github.com/MarvinBand/Migrations/issues/13) Creating code migration from service collection
- Extension method `AddMigration` to easy configure migration
- Passing logger to code migration

## [0.3.2] - 2019-10-31

### Added 

- Added method `ExecuteNonQueryScriptAsync` to `IDbProvider`

## [0.3.1] - 2019-09-16

### Fixed 

- Passing variables to code migrations

## [0.3] - 2019-09-16

### Added 

- Variables for scripts with auto substitution to script migrations 

## [0.2.16] - 2018-12-19

### Added

- Supports bath absolute and relative path for directories in code migrations provider
- Supports prefix for code migration provider

## [0.2.15] - 2018-12-18

### Fixed

- Returned `UseScriptPreMigrations`

## [0.2.14] - 2018-12-18

### Added

- Support script migrations embedded into specified assembly
- FluentAPI style for migrations providers
- Script migrations providers supports many directories as targets



## [0.2.13] - 2018-12-17

### Changed

- Fixing incorrect cotnracts in package

## [0.2.12] - 2018-12-17

### Changed

- Using `DbTransaction` instead `ComittableTransaction`

## [0.2.11] - 2018-12-17

### Added

- Each migration executed in transaction

## [0.2.10] - 2018-11-30

### Fixed

- Executing current version migration downgrade, stopping executing on target version

## [0.2.9] - 2018-11-15

### Fixed

- Error on check db version before pre-migrations

## [0.2.8] - 2018-11-09

### Added

- Added property `ConnectionString` to `IDbProvider`

## [0.2.7] - 2018-11-07

### Fixed

- Error logging

## [0.2.6] - 2018-11-07

### Changed

- Open connection after creating database

## [0.2.5] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package

## [0.2.4] - 2018-11-07

### Changed

- Creating `MigrationHistory` table after executing premigrations

## [0.2.3] - 2018-11-06

### Added

- `MigrationHistoryTableName` available as property of `IDbProvider`

## [0.2.2] - 2018-11-02

### Fixed

- Fixed nuget package target

## [0.2.1] - 2018-11-02

### Added

- Additional methods to check database and table existence

## [0.2] - 2018-11-02

### Added

- Added premigration scripts
- Added options for `IDbProvider` and factory to create `IDbProvider`

