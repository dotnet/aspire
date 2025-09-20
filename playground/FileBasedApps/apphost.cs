#:property IsAspireHost=true
#:property PublishAot=false

var builder = DistributedApplication.CreateBuilder(args);

builder.AddCSharpApp("api", "api.cs");

builder.Build().Run();
