// This file contains intentional compilation errors to test build failure scenarios

var builder = WebApplication.CreateBuilder(args);

// Intentional error: undefined variable
var undefinedVariable = nonExistentVariable;

// Intentional error: wrong type
string number = 123;

// Intentional error: missing semicolon
var app = builder.Build()

app.MapGet("/", () => "Hello World!");

app.Run();
