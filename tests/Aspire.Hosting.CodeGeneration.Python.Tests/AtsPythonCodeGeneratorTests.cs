// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;
using Xunit;

namespace Aspire.Hosting.CodeGeneration.Python.Tests;

public class AtsPythonCodeGeneratorTests
{
    private readonly global::Aspire.Hosting.CodeGeneration.Python.AtsPythonCodeGenerator _generator = new();

    [Fact]
    public void Language_ReturnsPython()
    {
        Assert.Equal("Python", _generator.Language);
    }

    [Fact]
    public void GenerateDistributedApplication_ReturnsExpectedFiles()
    {
        var context = new AtsContext
        {
            Capabilities = [],
            HandleTypes = [],
            DtoTypes = [],
            EnumTypes = []
        };

        var files = _generator.GenerateDistributedApplication(context);

        Assert.Contains("aspire.py", files.Keys);
        Assert.Contains("base.py", files.Keys);
        Assert.Contains("transport.py", files.Keys);
        Assert.Contains("create_builder", files["aspire.py"]);
    }
}
