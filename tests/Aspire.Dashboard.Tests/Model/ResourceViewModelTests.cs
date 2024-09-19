// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.ResourceService.Proto.V1;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceViewModelTests
{
    private static readonly DateTime s_dateTime = new DateTime(2000, 12, 30, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public void ToViewModel_EmptyEnvVarName_Success()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc",
            DisplayName = "TestName",
            CreatedAt = Timestamp.FromDateTime(s_dateTime),
            Environment =
            {
                new EnvironmentVariable { Name = string.Empty, Value = "Value!" }
            }
        };

        // Act
        var vm = resource.ToViewModel();

        // Assert
        Assert.Collection(resource.Environment,
            e =>
            {
                Assert.Empty(e.Name);
                Assert.Equal("Value!", e.Value);
            });
    }

    [Fact]
    public void ToViewModel_MissingRequiredData_FailWithFriendlyError()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc"
        };

        // Act
        var ex = Assert.Throws<InvalidOperationException>(resource.ToViewModel);

        // Assert
        Assert.Equal(@"Error converting resource ""TestName-abc"" to ResourceViewModel.", ex.Message);
        Assert.NotNull(ex.InnerException);
    }
}
