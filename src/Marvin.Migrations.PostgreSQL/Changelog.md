# Changelog

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