// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;

internal sealed class MockToolLambdaRuntimeEnvironment : ExecutableResource, ILambdaRuntimeEnvironment
{
    private readonly ILambdaFunctionMetadata _metadata;
    private readonly string _runtime;
    private readonly MockToolLambdaConfiguration _configuration;

    public MockToolLambdaRuntimeEnvironment(string name, ILambdaFunctionMetadata metadata,
        MockToolLambdaConfiguration configuration, string runtime) : base(name, "dotnet",
        metadata.OutputPath!)
    {
        _metadata = metadata;
        _runtime = runtime;
        _configuration = configuration;
        Annotations.Add(CreateArguments());
    }

    private CommandLineArgsCallbackAnnotation CreateArguments()
    {
        return new CommandLineArgsCallbackAnnotation(args =>
        {
            var suffix = _runtime switch
            {
                "dotnet8" => "8.0",
                "dotnet6" => "6.0",
                _ => throw new ArgumentOutOfRangeException(nameof(_runtime), _runtime, null) // Should never happen
            };

            args.Add($"lambda-test-tool-{suffix}");

            if (_configuration.DisableLaunchWindow)
            {
                args.Add("--no-launch-window");
            }

            args.Add("--port");
            args.Add($"{_configuration.Port}");
        });
    }

    public int Port => _configuration.Port;
    public string ProjectPath => _metadata.ProjectPath;
}
