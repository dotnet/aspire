using CatalogModel;
using CatalogService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapCatalogApi();
app.MapDefaultEndpoints();

app.Run();
