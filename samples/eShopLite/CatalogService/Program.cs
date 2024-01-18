using CatalogDb;
using CatalogService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure Npgsql.EntityFrameworkCore.PostgreSQL services
if (builder.Configuration.GetConnectionString("postgres") is string { } connectionString)
{
    builder.Services.AddNpgsqlDataSource(connectionString);
}

builder.Services.AddDbContextPool<CatalogDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql());

// Add the Aspire components for Npgsql.EntityFrameworkCore.PostgreSQL (health-check, tracing, metrics)
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.MapCatalogApi();
app.MapDefaultEndpoints();

app.Run();
