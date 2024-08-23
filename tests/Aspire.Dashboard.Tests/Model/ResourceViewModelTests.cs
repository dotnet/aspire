// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ResourceViewModelTests
{
    private static readonly DateTime s_dateTime = new(2000, 12, 30, 23, 59, 59, DateTimeKind.Utc);
    private static readonly BrowserTimeProvider s_timeProvider = new(NullLoggerFactory.Instance);

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
        var vm = resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup());

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
        var ex = Assert.Throws<InvalidOperationException>(() => resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup()));

        // Assert
        Assert.Equal(@"Error converting resource ""TestName-abc"" to ResourceViewModel.", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void ToViewModel_CopiesProperties()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc",
            DisplayName = "TestName",
            CreatedAt = Timestamp.FromDateTime(s_dateTime),
            Properties =
            {
                new ResourceProperty { Name = "Property1", Value = Value.ForString("Value1"), IsSensitive = false },
                new ResourceProperty { Name = "Property2", Value = Value.ForString("Value2"), IsSensitive = true }
            }
        };

        var kp = new KnownProperty("foo", "bar");

        // Act
        var viewModel = resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup(123, kp));

        // Assert
        Assert.Collection(
            viewModel.Properties,
            p =>
            {
                Assert.Equal("Property1", p.Key);
                Assert.Equal("Property1", p.Value.Name);
                Assert.Equal("Value1", p.Value.Value.StringValue);
                Assert.Equal(123, p.Value.Priority);
                Assert.Same(kp, p.Value.KnownProperty);
                Assert.False(p.Value.IsValueMasked);
                Assert.False(p.Value.IsValueSensitive);
            },
            p =>
            {
                Assert.Equal("Property2", p.Key);
                Assert.Equal("Property2", p.Value.Name);
                Assert.Equal("Value2", p.Value.Value.StringValue);
                Assert.Equal(123, p.Value.Priority);
                Assert.Same(kp, p.Value.KnownProperty);
                Assert.True(p.Value.IsValueMasked);
                Assert.True(p.Value.IsValueSensitive);
            });
    }
}
