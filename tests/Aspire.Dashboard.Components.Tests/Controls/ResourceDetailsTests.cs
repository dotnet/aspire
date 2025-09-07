// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
public class ResourceDetailsTests : DashboardTestContext
{
    [Fact]
    public async Task ClickMaskAllSwitch_UpdatedResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
            builder.Add(p => p.ResourceByName, new ConcurrentDictionary<string, ResourceViewModel>([new KeyValuePair<string, ResourceViewModel> (resource1.Name, resource1)]));
        });

        // Assert
        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            });

        var maskAllSwitch = cut.Find(".mask-all-switch");
        await maskAllSwitch.ClickAsync(new MouseEventArgs());

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.False(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar3", e.Name);
                Assert.False(e.IsValueMasked);
            });
    }

    [Fact]
    public async Task ClickMaskAllSwitch_NewResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
            builder.Add(p => p.ResourceByName, new ConcurrentDictionary<string, ResourceViewModel>([new KeyValuePair<string, ResourceViewModel> (resource1.Name, resource1)]));
        });

        // Assert
        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            });

        var maskAllSwitch = cut.Find(".mask-all-switch");
        await maskAllSwitch.ClickAsync(new MouseEventArgs());

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.False(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app2",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar3", e.Name);
                Assert.True(e.IsValueMasked);
            });
    }

    [Fact]
    public async Task ClickMaskEnvVarSwitch_UpdatedResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
            builder.Add(p => p.ResourceByName, new ConcurrentDictionary<string, ResourceViewModel>([new KeyValuePair<string, ResourceViewModel> (resource1.Name, resource1)]));
        });

        // Assert
        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            });

        var maskValueButton = cut.Find(".env-var-properties .grid-value-mask-button");
        await maskValueButton.ClickAsync(new MouseEventArgs());

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.True(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar3", e.Name);
                Assert.True(e.IsValueMasked);
            });
    }

    [Fact]
    public async Task ClickMaskEnvVarSwitch_NewResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
            builder.Add(p => p.ResourceByName, new ConcurrentDictionary<string, ResourceViewModel>([new KeyValuePair<string, ResourceViewModel> (resource1.Name, resource1)]));
        });

        // Assert
        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.Equal("value!", e.Value);
                Assert.True(e.IsValueMasked);
            });

        var maskValueButton = cut.Find(".env-var-properties .grid-value-mask-button");
        await maskValueButton.ClickAsync(new MouseEventArgs());

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.False(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.True(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app2",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.Equal("envvar1", e.Name);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar2", e.Name);
                Assert.True(e.IsValueMasked);
            },
            e =>
            {
                Assert.Equal("envvar3", e.Name);
                Assert.True(e.IsValueMasked);
            });
    }
}
