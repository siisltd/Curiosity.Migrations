# Curiosity.Migrations [![Build Status](https://travis-ci.org/MarvinBand/Migrations.svg?branch=master)](https://travis-ci.org/MarvinBand/Migrations) [![(License)](https://img.shields.io/github/license/siisltd/curiosity.migrations.svg)](https://github.com/siisltd/Curiosity.Mirgations/blob/master/LICENSE) [![NuGet Downloads](https://img.shields.io/nuget/dt/Curiosity.Migrations)](https://www.nuget.org/packages/Curiosity.Migrations) [![Documentation Status](https://readthedocs.org/projects/curiosity-migrations/badge/?version=latest)](https://curiosity-migrations.readthedocs.io/)


Database migration framework for .NET and .NET Core.

|Branch|Build status|
|---|---|
|master|[![Build Status](https://travis-ci.org/MarvinBand/Migrations.svg?branch=master)](https://travis-ci.org/MarvinBand/Migrations)|
|develop|[![Build Status](https://travis-ci.org/MarvinBand/Migrations.svg?branch=develop)](https://travis-ci.org/MarvinBand/Migrations)|

`Curiosity.Migrations` is a migration framework that uses SQL scripts and code migration to alter your database schema or seed a data.

Without migrations you need to create a lots of sql scripts that have to be run manually by every developer involved. 
Migrations solve the problem of evolving a database schema for multiple databases (for example, the developer's local database, the test database and the production database). 

# Features

`Curiosity.Migration` has a lot of useful features. You can find more information about them at special articles:

- [Script migrations](https://siisltdmigrations.readthedocs.io/features/script_migration/index.md): write your own DDL SQL scripts
  - [Batches](https://siisltdmigrations.readthedocs.io/features/script_migration/batches.md): separate a big SQL script into small batches 
- [Code migrations](https://siisltdmigrations.readthedocs.io/features/code_migration/index.md): manipulate data from C#, useful for database seeding
  - [Dependency Injection](https://siisltdmigrations.readthedocs.io/features/code_migration/di.md): inject dependencies into code migrations
  - [EntityFramework Integration](https://siisltdmigrations.readthedocs.io/features/code_migration/ef_integration.md): use `EntityFramework` for data manipulation from code migrations
- [Migration Providers](https://siisltdmigrations.readthedocs.io/features/migration_providers.md): store migrations in a different way (files, embedded resources, etc)
- [Variable substitutions](https://siisltdmigrations.readthedocs.io/features/variables.md): allows to insert some dynamic data to your migrations
- [Transactions](https://siisltdmigrations.readthedocs.io/features/transactions.md): you can enable or disable transaction for separate migration
- [Pre-migrations](https://siisltdmigrations.readthedocs.io/features/pre_migrations.md): executes SQL or code before main migration
- [Journal](https://siisltdmigrations.readthedocs.io/features/journal.md): choose your own table to store migration history
- [Downgrade migrations](https://siisltdmigrations.readthedocs.io/features/downgrade.md): allows you to reverse applied migrations

## Supported databases

<table>
  <tbody>
    <tr>
      <td align="center" valign="middle">
          <img src="https://raw.githubusercontent.com/siisltd/Curiosity.Migrations/master/docs/images/postgresql.png">
      </td>
    </tr>
  </tbody>
</table>

If you don't find a desired database, you can contribute and add support by yourself.

## Install

`Curiosity.Migrations` is available as a Nuget package.

| Package | Build Status | Version | Downloads |
|---------|------------|------------|------------|
| Curiosity.Migrations | [![Build Status](https://travis-ci.org/MarvinBand/Migrations.svg?branch=master)](https://travis-ci.org/MarvinBand/Migrations) | [![NuGet](https://img.shields.io/nuget/v/Curiosity.Migrations.svg)](https://www.nuget.org/packages/Curiosity.Migrations/) | [![NuGet](https://img.shields.io/nuget/dt/Curiosity.Migrations)](https://www.nuget.org/packages/Curiosity.Migrations) |
| Curiosity.Migrations.PostgreSQL | [![Build Status](https://travis-ci.org/MarvinBand/Migrations.svg?branch=master)](https://travis-ci.org/MarvinBand/Migrations) | [![NuGet](https://img.shields.io/nuget/v/Curiosity.Migrations.PostgreSQL.svg)](https://www.nuget.org/packages/Curiosity.Migrations.PostgreSQL/) | [![NuGet](https://img.shields.io/nuget/dt/Curiosity.Migrations.PostgreSQL)](https://www.nuget.org/packages/Curiosity.Migrations.PostgreSQL) |


## Getting help

You can find documentation and samples at [ReadTheDocs](https://curiosity-migrations.readthedocs.io/).