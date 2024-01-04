// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class SecretsTests
{
    [Fact]
    public void SecretResourceUnderDcpReadsValueFromConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Secrets:mysecretstore:mysecret"] = "MY_SECRET_VALUE";

        var secret = builder.AddSecretStore("mysecretstore").AddSecret("mysecret");
        var container = builder.AddContainer("mycontainer", "myimage").WithEnvironment("MY_SECRET", secret);

        var callbackAnnotations = container.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var context = new EnvironmentCallbackContext("dcp");

        foreach (var callbackAnnotation in callbackAnnotations)
        {
            callbackAnnotation.Callback(context);
        }

        Assert.Equal("MY_SECRET_VALUE", context.EnvironmentVariables["MY_SECRET"]);
    }
}
