using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureNpgsqlDataSource("db");

var app = builder.Build();

app.MapGet("/", (NpgsqlDataSource dataSource) =>
{
    using var conn = dataSource.OpenConnection();
    var command = conn.CreateCommand();
    command.CommandText = "SELECT 1";
    var result = command.ExecuteScalar();
    return result;
});

app.Run();
