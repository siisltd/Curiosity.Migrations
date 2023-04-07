# Changelog

## [4.0.0] - 2023-04-07

### Changed

- Upgraded to C# 11.0.
- Upgraded main package to `v4.0.0.`
- Changed migration history table default name.
- Replaced raw sql queries by SQL commands.

### Removed

- Removed `Dapper` usage.

## [3.0.1] - 2021-03-04

### Added

- Added creation of unique index on version for migration history table.

## [3.0.0] - 2020-12-08

### Changed

- Code style fixes.
- Improved performance.

### Removed

- DB state method.
- Major and minor migration polices.

## [2.2] - 2020-10-16

### Fixed

- Exceptions occurred during upgrade or downgrade were logged twice.

## [2.1] - 2020-09-21

### Changed

- Upgraded to C# 8.0. 

## [2.0] - 2020-05-14

### Changed

- Supports different strategy for migration history table analysis: is downgrade is enabled use version of last executed migration either max version of executed migrations.

## [1.2.0] - 2020-04-23

### Added

- Added logging of sql queries.

## [1.1.1] - 2020-04-01

### Changed

- Returning tasks instead of await them.

## [1.1] - 2020-03-24

- Added cancellation token support.

## [1.0] - 2020-02-11

Moved to [SIIS Ltd](https://github.com/SIIS-Ltd/Curiosity.Migrations).
- [#8](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/8) Migration history table now really contains migrations history.
- [#9](https://github.com/SIIS-Ltd/Curiosity.Migrations/issues/9) Check correctness of current db version before migrations start.

## [0.4] - 2019-10-31

### Changed 

- Upgraded with main packages.

### Changed

- Reduced code duplication on query execution.

## [0.3.1] - 2019-10-31

### Added 

- Added implementation of `ExecuteNonQueryScriptAsync` method from `IDbProvider`.

### Changed

- Reduced code duplication on query execution.

## [0.3] - 2019-09-16

### Added 

- Variables for scripts with auto substitution to script migrations. 

## [0.2.15] - 2018-12-18

### Changed

- `varchar(10)` instead `text` for `version` column in history table.

### Fixed

- Incorrect error message on script error.

## [0.2.14] - 2018-12-17

### Changed

- Fixing incorrect contracts in package.

## [0.2.13] - 2018-12-17

### Changed

- Using `DbTransaction` instead `ComittableTransaction`.

## [0.2.12] - 2018-12-17

### Added

- Each migration executed in transaction.

## [0.2.10] - 2018-11-13

### Changed

- Default values for `PostgreDbProviderOptions` replaced by `null`. Now `PostgreDbProvider` is using default values from DataBase.

## [0.2.9] - 2018-11-09

### Added

- Added property `ConnectionString` to `PostgreDbProvider`.

## [0.2.8] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package.

## [0.2.7] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package.

## [0.2.6] - 2018-11-07

### Changed

- Assert connections only for script with initial catalogue.

## [0.2.5] - 2018-11-06

### Fixed

- Fixed `StackOverflowException` in  `PostgreDbProvider`.

## [0.2.4] - 2018-11-06

### Added

- `MigrationHistoryTableName` available as property of `IDbProvider`.

## [0.2.3] - 2018-11-06

### Fixed

- Fixed `ArgumentNullException` in  `PostgreDbProviderOptions`.

## [0.2.2] - 2018-11-02

### Fixed

- Fixed nuget package target.

## [0.2.1] - 2018-11-02

### Added

- Additional methods to check database and table existence.

## [0.2] - 2018-11-02

### Added

- Added factory to create `PostgreDbProvider`.
- Added `PostgreDbProviderOptions` to configure provider.

### Changed

- Using one connection for `IDbProvider` instead creation new one per script execution.
