# Changelog

## [4.2.1] - 2023-11-13

### Added

- Options for migration dependencies.
```
--CURIOSITY:Dependencies=Major1.Minor1 Major2.Minor2
```

### Changed

- New parameter at ScriptMigration constructor.

## [4.2.0] - 2023-04-21

### Changed

- Improved detecting incorrect migration files names.

## [4.1.3] - 2023-04-14

### Changed

- Renamed cancellation token param.

## [4.1.2] - 2023-04-12

### Fixed

- Fixed processing script migrations prefix.

## [4.1.1] - 2023-04-12

### Fixed

- Fixed migrations order at engines constructor.

## [4.1.0] - 2023-04-12

### Added

- Added `ScriptIncorrectNamingAction` for `ScriptProvider` to specify what should migrator do with scripts with incorrect name.

## [4.0.1] - 2023-04-10

### Added

- Added README.md to a package.

### Changed

- Downgraded MS packages to `6.0.0` version.

## [4.0.0] - 2023-04-10

### Added

- Added more comments.
- Added options to `ServiceCollectionExtensions.AddMigrations` to control whether current instance of IServiceCollection should be used as default for dependency injection of migrator.
- Added `onlyTargetVersion` option.
- Added saving initial version format (before parse changes).

### Changed

- Upgraded to C# 11.0.
- Refactored `MigrationPolicy`, changed existed enum values codes: improved extensibility for future changes. 
- Renamed `MigrationError`->`MigrationErrorCode`.
- Refactored `MigrationErrorCode`, changed existed enum values codes: improved extensibility for future changes.
- [#22](https://github.com/siisltd/Curiosity.Migrations/issues/22): Forbid to use `-` at version's major part. Replaced it by `_`.
- [#27](https://github.com/siisltd/Curiosity.Migrations/issues/27): Replaced `MigrateAsync` by explicit `UpgradeDatabaseAsync` and `DowngradeDatabaseAsync` methods.
- [#28](https://github.com/siisltd/Curiosity.Migrations/issues/28): Renamed `DbVersion` -> `MigrationVersion`.

### Removed

- Removed `Dapper` dependency. 
- Moved `MassUpdateCodeMigrationBase` to `Curiosity.Migration.Utils` package.

### Fixed
 
- [#22](https://github.com/siisltd/Curiosity.Migrations/issues/22): Fixed issue with parsing version like `20210916-1055-00`.

## [3.1.0] - 2022-07-29

### Added

- Added `MassUpdateCodeMigrationBase` for mass updates.

## [3.0.7] - 2022-04-21

### Added

- Added option to disabling warn about incorrect script migrations naming.

## [3.0.6] - 2022-03-01

### Fixed

- Fixed log message.

## [3.0.5] - 2022-03-01

### Fixed

- Fixed applying migrations via patch strategy.

## [3.0.4] - 2021-08-03

### Changed

- Fixed transaction block parsing.

## [3.0.3] - 2021-03-08

### Changed

- Updated package info.

## [3.0.2] - 2020-12-10

### Changed

- Changed level of log for incorrect migration name from `Warning` to `Debug`.

## [3.0.1] - 2020-12-09

### Fixed

- Version parsing. 

## [3.0.0] - 2020-12-08

### Changed

- Code style fixes.
- Changed migration applying strategy from sequential to patch. 

### Removed

- DB state method.
- Major and minor migration polices.

## [2.2] - 2020-10-16

### Fixed

- Exceptions occurred during upgrade or downgrade were logged twice.

## [2.1] - 2020-09-21

### Added

- Options for each migration. First option is switching of transaction for specific migration.
```
--CURIOSITY:Transactions=off
```

### Changed

- Upgraded to C# 8.0.

## [2.0] - 2020-05-14

### Changed

- Split downgrade migrations into another interface for safety reasons.
- Default `downgrade policy` is `Forbidden`.
- Supports different strategy for migration history table analysis.

## [1.2.0] - 2020-04-23

### Added
- Added logging of sql queries.

### Removed

- Removed dependency from `JetBrains.Annotations`.

## [1.1.1] - 2020-04-01

### Added
- Added extended logging for main migration process.
- Added cancellation token to pre-migrations.

## [1.1] - 2020-03-24

- Added cancellation token support.
- Fixed batches creation when creating an empty batch as the first one.

## [1.0] - 2020-02-11

Moved to [SIIS Ltd](https://github.com/SIIS-Ltd/Curiosity.Migrations).
- [#6](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/6) Added support of batches inside script migrations.
- [#7](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/7) Added running script migration name to log output.
- [#8](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/8) Migration history table now really contains migrations history.
- [#9](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/9) Check correctness of current db version before migrations start.

## [0.4] - 2019-10-31

### Added 

- [#13](https://github.com/MarvinBand/Migrations/issues/13) Creating code migration from service collection.
- Extension method `AddMigration` to easy configure migration.
- Passing logger to code migration.

## [0.3.2] - 2019-10-31

### Added 

- Added method `ExecuteNonQueryScriptAsync` to `IDbProvider`.

## [0.3.1] - 2019-09-16

### Fixed 

- Passing variables to code migrations.

## [0.3] - 2019-09-16

### Added 

- Variables for scripts with auto substitution to script migrations. 

## [0.2.16] - 2018-12-19

### Added

- Supports bath absolute and relative path for directories in code migrations provider.
- Supports prefix for code migration provider.

## [0.2.15] - 2018-12-18

### Fixed

- Returned `UseScriptPreMigrations`.

## [0.2.14] - 2018-12-18

### Added

- Support script migrations embedded into specified assembly.
- FluentAPI style for migrations providers.
- Script migrations providers supports many directories as targets.

## [0.2.13] - 2018-12-17

### Changed

- Fixing incorrect contracts in a Nuget package.

## [0.2.12] - 2018-12-17

### Changed

- Using `DbTransaction` instead `ComittableTransaction`.

## [0.2.11] - 2018-12-17

### Added

- Each migration executed in transaction.

## [0.2.10] - 2018-11-30

### Fixed

- Executing current version migration downgrade, stopping executing on target version.

## [0.2.9] - 2018-11-15

### Fixed

- Error on check db version before pre-migrations.

## [0.2.8] - 2018-11-09

### Added

- Added property `ConnectionString` to `IDbProvider`.

## [0.2.7] - 2018-11-07

### Fixed

- Error logging.

## [0.2.6] - 2018-11-07

### Changed

- Open connection after creating database.

## [0.2.5] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package.

## [0.2.4] - 2018-11-07

### Changed

- Creating `MigrationHistory` table after executing pre-migrations.

## [0.2.3] - 2018-11-06

### Added

- `MigrationHistoryTableName` available as property of `IDbProvider`

## [0.2.2] - 2018-11-02

### Fixed

- Fixed nuget package target.

## [0.2.1] - 2018-11-02

### Added

- Additional methods to check database and table existence.

## [0.2] - 2018-11-02

### Added

- Added pre-migration scripts
- Added options for `IDbProvider` and factory to create `IDbProvider`.
