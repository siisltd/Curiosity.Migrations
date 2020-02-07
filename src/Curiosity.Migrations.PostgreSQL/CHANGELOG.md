# Changelog

## [1.0] - 2019-11-18

Moved to [SIIS Ltd](https://github.com/SIIS-Ltd/Migrations).

## [0.4] - 2019-10-31

### Changed 

- Upgraded with main packages

### Changed

- Reduced code duplication on query execution

## [0.3.1] - 2019-10-31

### Added 

- Added implementation of `ExecuteNonQueryScriptAsync` method from `IDbProvider`

### Changed

- Reduced code duplication on query execution

## [0.3] - 2019-09-16

### Added 

- Variables for scripts with auto substitution to script migrations 

## [0.2.15] - 2018-12-18

### Changed

- `varchar(10)` instaed `text` for `version` column in history table

### Fixed

- Incorrect error message on script error

## [0.2.14] - 2018-12-17

### Changed

- Fixing incorrect cotnracts in package

## [0.2.13] - 2018-12-17

### Changed

- Using `DbTransaction` instead `ComittableTransaction`

## [0.2.12] - 2018-12-17

### Added

- Each migration executed in transaction

## [0.2.10] - 2018-11-13

### Changed

- Default values for `PostgreDbProviderOptions` replaced by `null`. Now `PostgreDbProvider` is using default values from DataBase

## [0.2.9] - 2018-11-09

### Added

- Added property `ConnectionString` to `PostgreDbProvider`

## [0.2.8] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package

## [0.2.7] - 2018-11-07

### Fixed

- Fixed release notes link in nuget package

## [0.2.6] - 2018-11-07

### Changed

- Assert connections only for script with initial catalogue.  

## [0.2.5] - 2018-11-06

### Fixed

- Fixed `StackOverflowException` in  `PostgreDbProvider`

## [0.2.4] - 2018-11-06

### Added

- `MigrationHistoryTableName` available as property of `IDbProvider`

## [0.2.3] - 2018-11-06

### Fixed

- Fixed `ArgumentNullException` in  `PostgreDbProviderOptions`

## [0.2.2] - 2018-11-02

### Fixed

- Fixed nuget package target

## [0.2.1] - 2018-11-02

### Added

- Additional methods to check database and table existence

## [0.2] - 2018-11-02

### Added

- Added factory to create `PostgreDbProvider`
- Added `PostgreDbProviderOptions` to configure provider

### Changed

- Using one connection for `IDbProvider` instead creation new one per script execution