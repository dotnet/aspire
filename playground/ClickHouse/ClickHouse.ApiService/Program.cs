using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.UseHttpsRedirection();

var connectionString = builder.Configuration.GetConnectionString("default");

app.MapGet("/version", async () =>
{
    using var connection = new ClickHouseConnection(connectionString);
    var version = await connection.ExecuteScalarAsync("SELECT version()");
    return version.ToString();
});

app.MapGet("/table-functions", async () =>
{
    using var connection = new ClickHouseConnection(connectionString);
    var sql = "SELECT * FROM system.table_functions";
    var functions = await connection.QueryAsync<string>(sql);
    return string.Join('\n', functions);
});

app.Run();
