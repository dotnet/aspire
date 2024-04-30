var builder = DistributedApplication.CreateBuilder(args)
    .AddAWSLambdaToolsSupport();

// builder.DisableMockToolLambda();

builder.AddLambdaFunction<LambdaFunctions.LambdaRestApi>("api");

builder.AddLambdaFunction<LambdaFunctions.ExecutableLambdaHttpApi>("executableApi", config =>
{
    // config.Disabled = true;
});

builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_OrderFunction_HandleAsync>("orderFunction", config =>
    {
        // config.Port = 8090;
        config.DisableLaunchWindow = true;
    })
    .SetAsDefaultFunctionInProject();
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_EmailFunction_InformCustomer>("sendEmail")
    .WithEnvironment("SEND_FROM","no-reply@example.com");
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_Authorizer_Authorize>("authorizer");
builder.AddLambdaFunction<LambdaFunctions.ClassLibraryFunctions_Functions_S3Function_Handle>("s3Function");

// builder.AddLambdaFunction("nodeFunction", LambdaRuntime.Custom("nodejs20.x"), "index.handler", "./node-app");

builder.Build().Run();
