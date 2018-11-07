# Changelog

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