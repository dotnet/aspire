# Data migrations playground

To use migrations:

- Make changes to `MyDb1Context` in `DatabaseMigration.ApiModel`. For example, add a new property to entry.
- Create a new migration by executing the following command in the `DatabaseMigration.ApiService` directory:
  ```powershell
  dotnet ef migrations add MyNewMigration --project ..\DatabaseMigration.ApiModel\DatabaseMigration.ApiModel.csproj
  ```
- Migrations are applied automatically by the `DatabaseMigration.MigrationService` app when `DatabaseMigration.AppHost` is started.
