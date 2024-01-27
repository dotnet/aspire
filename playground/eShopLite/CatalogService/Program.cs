using CatalogDb;
using CatalogService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("catalogdb")
    ?? throw new InvalidOperationException("Connection string is not configured.");

// Configure Npgsql.EntityFrameworkCore.PostgreSQL services
builder.Services.AddDbContextPool<CatalogDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql(connectionString))
    .EnrichNpgsqlEntityFrameworkCore<CatalogDbContext>(builder);

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
