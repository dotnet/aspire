using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.AddSqlServerClient("todosdb");

var app = builder.Build();

app.MapGet("/", async (SqlConnection connection) =>
{
    await connection.OpenAsync();
    using var command = new SqlCommand("""
        IF NOT EXISTS (
            SELECT * FROM sys.tables t
            JOIN sys.schemas s ON (t.schema_id = s.schema_id)
            WHERE s.name = 'dbo' AND t.name = 'Tasks'
        )
        CREATE TABLE dbo.Tasks (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Text nvarchar(max)
        );
        """, connection);
    await command.ExecuteNonQueryAsync();

    return Results.Ok("Table created or already exists.");
});

app.MapGet("/new", async (SqlConnection connection) =>
{
    // Create a new record in the Tasks table
    await connection.OpenAsync();
    using var command = new SqlCommand("INSERT INTO dbo.Tasks (Text) VALUES ('New Task'); SELECT SCOPE_IDENTITY();", connection);
    var id = await command.ExecuteScalarAsync();

    return Results.Ok($"New task created with ID: {id}");
});

app.Run();
