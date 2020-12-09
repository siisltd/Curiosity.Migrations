# Basics

There is some basic concepts you should know about `Curiosity.Migrations`.

## Versioning 

`Curiosity.Migrations` allows you to use different ways to versioning migrations. But version must me sortable and comparable: migrator sort migrations by version to apply the in a correct order.

Version consist of two parts - major and minor numbers separated by dot - `Major.Minor`.
`Major` is required and usually uses to separate different migrations. `Minor` is optional and uses to combine migrations into a logical group which must be applied sequentially.

Commonly, version is just an incrementing value. Some people use a single number to versioning their migrations, another use numbered date format such as `yyyyMMdd`. `Curiosity.Migrations` uses next regular expression pattern to parse version from string:

> `([\d|-]+)(\.(\d+))*`

There are examples of valid versions:

- `1` 
- `100.5` 
- `20201012` 
- `20201012-1030` 
- `20201012-1030.21` 

## Policy 

## Configuration

