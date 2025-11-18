var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var dts = builder.AddContainer("dts", "mcr.microsoft.com/dts/dts-emulator", "latest")
    .WithEndpoint(name: "grpc", targetPort: 8080)
    .WithEndpoint(name: "http", targetPort: 8082);

var grpcEndpoint = dts.GetEndpoint("grpc");

ReferenceExpression dtsConnectionString = ReferenceExpression.Create($"Endpoint=http://{grpcEndpoint.Property(EndpointProperty.Host)}:{grpcEndpoint.Property(EndpointProperty.Port)};Authentication=None");

builder.AddAzureFunctionsProject<Projects.AzureFunctionsWithDts_Functions>("funcapp")
    .WithHostStorage(storage)
    .WaitFor(dts)
    .WithEnvironment("DURABLE_TASK_SCHEDULER_CONNECTION_STRING", dtsConnectionString)
    .WithEnvironment("TASKHUB_NAME", "default");

builder.Build().Run();
