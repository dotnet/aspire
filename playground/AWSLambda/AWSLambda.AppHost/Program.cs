var builder = DistributedApplication.CreateBuilder(args)
    .AddAWSLambdaToolsSupport();

// builder.DisableMockToolLambda();

builder.AddLambdaFunction<LambdaFunctions.LambdaRestApi>("api");

builder.AddLambdaFunction<LambdaFunctions.ExecutableLambdaHttpApi>("executableApi", config =>
{
    // Mock Tool Config (Optional).

    // config.Disabled = true;
    // config.Port = 8090;
    // config.EnableLaunchWindow = true;
});

builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_OrderFunction_HandleAsync>("orderFunction", _ =>
    {
        // When Class Library, Mock Tool Config must be set in the first Lambda Function created for the project.
    })
    .SetAsDefaultFunctionInProject();
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_EmailFunction_InformCustomer>("sendEmail")
    .WithEnvironment("SEND_FROM","no-reply@example.com");
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_Authorizer_Authorize>("authorizer");
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_Functions_S3Function_Handle>("s3Function");

// builder.AddLambdaFunction("nodeFunction", LambdaRuntime.Custom("nodejs20.x"), "index.handler", "./node-app");

builder.Build().Run();
