# The idea behind of Curiosity.Migrations

## Migration as Code
Curiosity.Migrations embraces the migration-as-code philosophy, providing version-controlled database schema and data changes. This approach ensures complete history tracking of all database modifications, consistent versioning across environments, and maintains database schema consistency with your application code.

## Raw SQL Migrations
The library prioritizes raw SQL migrations, giving developers precise control over data transformations. This approach is especially valuable for projects with large production tables where performance and efficiency are critical. Curiosity.Migrations expects developers to have SQL knowledge, empowering them to write optimized database operations rather than relying on auto-generated queries.

## Code Migrations
For complex data manipulation scenarios, Curiosity.Migrations supports code-based migrations. This feature allows developers to leverage the full power of C# when SQL alone is insufficient for complex transformation logic, data validation, or when integrating with external systems during migration.

## Safety in Production
Curiosity.Migrations implements robust safety mechanisms for production environments, including:
- Configurable migration policies to prevent destructive operations in production
- Dependency management ensuring migrations run in the correct order
- Validation checks before applying changes to production databases
- Rollback capabilities for failed migrations

## Migration Progress Monitoring
The library provides built-in monitoring capabilities to track migration progress, especially important for long-running migrations on large datasets. This includes detailed logging, progress reporting, and diagnostic information to help identify and resolve issues quickly.

## Testability
Curiosity.Migrations simplifies database testing by making it easy to create and initialize test databases with specific migration states. This enables developers to test their applications against different database versions and configurations, ensuring compatibility across schema changes.

## Comparison to C# Alternatives

Curiosity.Migrations offers distinct advantages when compared to other .NET database migration tools:

### Entity Framework Core Migrations
- **EF Core**: Generates migrations from code-first models, abstracting SQL details
- **Curiosity.Migrations**: Provides direct SQL control for optimal performance and precision
- **Difference**: Better suited for performance-critical systems where ORM-generated SQL may not be optimal

### FluentMigrator
- **FluentMigrator**: Uses a fluent C# API to define schema changes
- **Curiosity.Migrations**: Combines raw SQL with code-based migrations for better flexibility
- **Difference**: More explicit control over complex data transformations

### DbUp
- **DbUp**: Simple SQL script runner with versioning
- **Curiosity.Migrations**: Adds code migration capabilities and better production safeguards
- **Difference**: Enhanced safety features and monitoring for large-scale production deployments

### Evolve
- **Evolve**: Flyway-inspired .NET migration tool
- **Curiosity.Migrations**: Offers deeper integration with .NET testing infrastructure
- **Difference**: Superior testability and monitoring capabilities

Our approach balances the precision of direct SQL with the power of C# code migrations, while prioritizing safety, monitoring, and testability for enterprise-grade applications.