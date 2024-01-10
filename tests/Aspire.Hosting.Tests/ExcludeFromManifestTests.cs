// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ExcludeFromManifestTests
{
    [Fact]
    public void CanExcludeFromManifestWithCallbackAnnotation()
    {
        var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder.WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore)
            .ExcludeFromManifest();
    }

    private static TestProgram CreateTestProgram() => TestProgram.Create<ExcludeFromManifestTests>();
}
