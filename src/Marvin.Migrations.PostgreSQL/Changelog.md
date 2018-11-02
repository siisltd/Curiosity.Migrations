# Changelog

## [0.2] - 2018-11-02

### Add

- Added factory to create `PostgreDbProvider`
- Added `PostgreDbProviderOptions` to configure provider

### Changed

- Using one connection for `IDbProvider` instead creation new one per script execution