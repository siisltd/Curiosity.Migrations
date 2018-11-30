# Changelog

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

