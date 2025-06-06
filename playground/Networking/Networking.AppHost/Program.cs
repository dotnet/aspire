var builder = DistributedApplication.CreateBuilder(args);

// S1: Container to container communication with endpoint
{
    // The HTTP endpoint is also available from the host machine at http://localhost:8080
    var s1Echo = builder.AddContainer("s1-echo", "hashicorp/http-echo")
        .WithHttpEndpoint(8080, targetPort: 5678);
    var s1endpoint = s1Echo.GetEndpoint("http");
    s1Echo.WithEnvironment("ECHO_TEXT", $"Hello from {s1endpoint}");

    builder.AddContainer("s1-http-client", "curlimages/curl")
        .WithArgs("-c", "sleep 1 && curl \"$TARGET_URL\"")
        .WithEntrypoint("/bin/sh")
        .WithEnvironment("TARGET_URL", s1endpoint)
        .WithEnvironment(c => c.EnvironmentVariables.Add("TARGET_URL_ALT", s1endpoint.Url))
        .WaitFor(s1Echo);
}

// S2: Container to container communication without endpoint
{
    var s2Echo = builder.AddContainer("s2-echo", "hashicorp/http-echo");
    s2Echo.WithEnvironment("ECHO_TEXT", $"Hello from http://{s2Echo.Resource.Name}:5678");

    builder.AddContainer("s2-http-client", "curlimages/curl")
        .WithArgs("-c", "sleep 1 && curl \"$TARGET_URL\"")
        .WithEntrypoint("/bin/sh")
        .WithEnvironment("TARGET_URL", $"http://{s2Echo.Resource.Name}:5678")
        .WaitFor(s2Echo);
}

// S3: Container to host communication
{
    var s3Echo = builder.AddProject<Projects.Networking_Echo>("s3-echo", launchProfileName: null)
        .WithHttpEndpoint(5158);
    var s3endpoint = s3Echo.GetEndpoint("http");
    s3Echo.WithEnvironment("ECHO_TEXT", $"Hello from {s3endpoint}");

    builder.AddContainer("s3-http-client", "curlimages/curl")
        .WithArgs("-c", "sleep 1 && curl \"$TARGET_URL\"")
        .WithEntrypoint("/bin/sh")
        .WithEnvironment("TARGET_URL", s3endpoint)
        .WithEnvironment(c => c.EnvironmentVariables.Add("TARGET_URL_ALT", s3endpoint.Url))
        .WaitFor(s3Echo);
}

// S4: Host to host communication
{
    var s4Echo = builder.AddProject<Projects.Networking_Echo>("s4-echo", launchProfileName: null)
        .WithHttpEndpoint(5159);
    var s4endpoint = s4Echo.GetEndpoint("http");
    s4Echo.WithEnvironment("ECHO_TEXT", $"Hello from {s4endpoint}");

    builder.AddProject<Projects.Networking_HttpClient>("s4-http-client")
        .WithEnvironment("TARGET_URL", s4endpoint)
        .WithEnvironment(c => c.EnvironmentVariables.Add("TARGET_URL_ALT", s4endpoint.Url))
        .WaitFor(s4Echo);
}

// S5: Host to container communication
{
    var s5Echo = builder.AddContainer("s5-echo", "hashicorp/http-echo")
        .WithHttpEndpoint(8081, targetPort: 5678);
    var s5endpoint = s5Echo.GetEndpoint("http");
    s5Echo.WithEnvironment("ECHO_TEXT", $"Hello from {s5endpoint}");

    builder.AddProject<Projects.Networking_HttpClient>("s5-http-client")
        .WithEnvironment("TARGET_URL", s5endpoint)
        .WithEnvironment(c => c.EnvironmentVariables.Add("TARGET_URL_ALT", s5endpoint.Url))
        .WaitFor(s5Echo);
}

builder.Build().Run();
