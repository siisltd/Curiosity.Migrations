# Changelog

## [4.0.0-beta1] - 2023-05-01

### Added

- Initial release of the MS SQL Server migration provider
- Support for creating and managing databases in MS SQL Server
- Schema-aware operations with support for custom schema names
- Enhanced SQL Server-specific configuration options:
  - File placement and sizing (data and log files)
  - Collation settings
  - Performance options (initial size, growth settings, max size)
  - Transaction isolation level configuration
- Improved error handling with SQL Server-specific error codes
- Better SQL command logging
- Complete support for migration versioning
- Migration history table with combined version field
- Automatic schema creation when needed 