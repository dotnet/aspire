// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[Trait("Partition", "5")]
public class BrowserUrlReferenceTests
{
    [Fact]
    public async Task ManifestUsesPublisherExpressionForDirectAndInterpolatedValues()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var publicHost = builder.AddParameter("public-host");
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var origin = frontend.GetBrowserOrigin("http")
            .WithPublishExpression(ReferenceExpression.Create($"https://{publicHost}"));
        builder.AddContainer("backend", "image")
            .WithEnvironment("ORIGIN", origin)
            .WithEnvironment("CONFIG", ReferenceExpression.Create($"origin={origin}"));
        using var app = builder.Build();

        await Verify((await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>())).ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
    }

    [Fact]
    public async Task ManifestUsesValueResourcesForCollectionAndConditionalPublishingExpressions()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var publicOrigins = builder.AddParameter("public-origins");
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var browser = frontend.GetBrowserOrigin("http");
        var joined = browser.Origins.Join(";").WithPublishExpression(ReferenceExpression.Create($"{publicOrigins}"));
        var json = browser.Origins.AsJson().WithPublishExpression(ReferenceExpression.Create($"{publicOrigins}"));
        var conditional = ReferenceExpression.CreateConditional(frontend.GetEndpoint("http").Property(EndpointProperty.TlsEnabled),
            bool.TrueString, ReferenceExpression.Create($"https://deployed.example"), ReferenceExpression.Create($"http://deployed.example"));
        var conditionalOrigin = browser.WithPublishExpression(conditional);
        builder.AddContainer("backend", "image")
            .WithEnvironment("ORIGINS", joined)
            .WithEnvironment("JSON", ReferenceExpression.Create($"allowed={json}"))
            .WithEnvironment("CONDITIONAL", conditionalOrigin);
        using var app = builder.Build();

        await Verify((await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>())).ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
    }

    [Fact]
    public async Task ManifestConditionalBrowserBranchesRetainDeploymentReferences()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var publicHost = builder.AddParameter("public-host");
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var browser = frontend.GetBrowserOrigin("http")
            .WithPublishExpression(ReferenceExpression.Create($"https://{publicHost}"));
        var tls = frontend.GetEndpoint("http").Property(EndpointProperty.TlsEnabled);
        var conditional = ReferenceExpression.CreateConditional(tls, bool.FalseString,
            ReferenceExpression.Create($"{browser}/signin-oidc"), ReferenceExpression.Create($"https://fallback.example/signin-oidc"));
        var nested = ReferenceExpression.CreateConditional(tls, bool.FalseString,
            conditional, ReferenceExpression.Empty);
        builder.AddContainer("backend", "image")
            .WithEnvironment("REDIRECT", conditional)
            .WithEnvironment("NESTED", nested);
        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>()).DefaultTimeout();

        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
        Assert.False(BrowserUrlSnapshotAnnotation.GetOrCreate(frontend.Resource).Value.IsValueSet);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ManifestInactiveBrowserBranchDoesNotRequirePublishingSupport(bool browserInTrueBranch)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var browser = ReferenceExpression.Create($"{frontend.GetBrowserOrigin("http")}");
        var deployed = ReferenceExpression.Create($"https://deployed.example");
        var conditional = ReferenceExpression.CreateConditional(
            frontend.GetEndpoint("http").Property(EndpointProperty.TlsEnabled),
            browserInTrueBranch ? bool.TrueString : bool.FalseString,
            browserInTrueBranch ? browser : deployed,
            browserInTrueBranch ? deployed : browser);
        builder.AddContainer("backend", "image").WithEnvironment("ORIGIN", conditional);
        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>()).DefaultTimeout();

        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json")
            .UseParameters(browserInTrueBranch);
        Assert.False(BrowserUrlSnapshotAnnotation.GetOrCreate(frontend.Resource).Value.IsValueSet);
    }

    [Fact]
    public async Task ManifestPreservesShortCircuitingForUnselectedNestedConditions()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var unselectedCondition = new ParameterResource("unselected-condition", _ =>
            throw new InvalidOperationException("An unselected condition must not be evaluated."));
        var selectedValue = new ParameterResource("selected-value", _ => "selected");
        var nested = ReferenceExpression.CreateConditional(unselectedCondition, bool.TrueString,
            ReferenceExpression.Create($"nested-true"), ReferenceExpression.Create($"nested-false"));
        var conditional = ReferenceExpression.CreateConditional(
            frontend.GetEndpoint("http").Property(EndpointProperty.TlsEnabled), bool.TrueString,
            nested, ReferenceExpression.Create($"{selectedValue}"));
        builder.AddContainer("backend", "image").WithEnvironment("RESULT", conditional);
        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>()).DefaultTimeout();

        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
    }

    [Fact]
    public async Task ManifestConditionalBrowserConditionReceivesPublishingContext()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var browser = frontend.GetBrowserOrigin("http")
            .WithPublishExpression(ReferenceExpression.Create($"https://deployed.example"));
        var conditional = ReferenceExpression.CreateConditional(browser, "https://deployed.example",
            ReferenceExpression.Create($"selected"), ReferenceExpression.Create($"not-selected"));
        builder.AddContainer("backend", "image").WithEnvironment("RESULT", conditional);
        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>()).DefaultTimeout();

        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
        Assert.False(BrowserUrlSnapshotAnnotation.GetOrCreate(frontend.Resource).Value.IsValueSet);
    }

    [Fact]
    public void ValueExpressionReferencesABoundedValueResource()
    {
        var browser = new BrowserOriginReference(new ContainerResource(new string('a', 64)), "https");
        var configured = browser.WithPublishExpression(ReferenceExpression.Create($"https://deployed.example"));

        Assert.Equal($"{{{browser.ManifestResourceName}.connectionString}}", browser.ValueExpression);
        Assert.InRange(browser.ManifestResourceName.Length, 1, ModelName.DefaultMaxLength);
        Assert.NotEqual(browser.ManifestResourceName, configured.ManifestResourceName);
        Assert.NotEqual(configured.ManifestResourceName,
            browser.WithPublishExpression(ReferenceExpression.Create($"https://deployed.example")).ManifestResourceName);
        Assert.NotEqual(browser.Origins.Join(";").ManifestResourceName, browser.Origins.AsJson().ManifestResourceName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManifestBrowserInstancesPreserveFormattingAndWrappedTokens(bool reverseOrder)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var host = builder.AddParameter("host");
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var browser = frontend.GetBrowserOrigin("http");
        var raw = browser.WithPublishExpression(ReferenceExpression.Create($"https://example/?value={host}"));
        var formatted = browser.WithPublishExpression(ReferenceExpression.Create($"https://example/?value={host:uri}"));
        Assert.NotEqual(raw.ValueExpression, formatted.ValueExpression);
        var wrapped = new TestManifestValue($"raw={raw.ValueExpression}|encoded={formatted.ValueExpression}", (_, _) =>
            throw new InvalidOperationException("A wrapper around browser references must stay symbolic."), raw, formatted);
        var backend = builder.AddContainer("backend", "image");
        if (reverseOrder)
        {
            backend.WithEnvironment("ENCODED", formatted).WithEnvironment("RAW", raw);
        }
        else
        {
            backend.WithEnvironment("RAW", raw).WithEnvironment("ENCODED", formatted);
        }
        backend.WithEnvironment("WRAPPED", wrapped)
            .WithEnvironment("REPEATED", ReferenceExpression.Create($"{raw}|{formatted}|{raw}"));
        builder.AddContainer("callback", "unused").WithManifestPublishingCallback(context =>
        {
            context.Writer.WriteString("type", "value.v0");
            context.Writer.WriteString("connectionString", $"{raw.ValueExpression}|{formatted.ValueExpression}");
            context.TryAddDependentResources(raw);
            context.TryAddDependentResources(formatted);
        });
        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var manifest = await ManifestUtils.GetManifestForModel(model);
        var repeated = await ManifestUtils.GetManifestForModel(model);

        Assert.True(JsonNode.DeepEquals(manifest, repeated));
        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json")
            .UseParameters(reverseOrder);
    }

    [Fact]
    public async Task ManifestBrowserInstancesPreserveDistinctPublishingProviders()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var firstCalls = 0;
        var secondCalls = 0;
        var first = new TestManifestValue("{opaque}", (_, _) =>
        {
            firstCalls++;
            return ValueTask.FromResult<string?>("https://first.example");
        });
        var second = new TestManifestValue("{opaque}", (_, _) =>
        {
            secondCalls++;
            return ValueTask.FromResult<string?>("https://second.example");
        });
        var browser = frontend.GetBrowserOrigin("http");
        var firstBrowser = browser.WithPublishExpression(ReferenceExpression.Create($"{first}"));
        var secondBrowser = browser.WithPublishExpression(ReferenceExpression.Create($"{second}"));
        builder.AddContainer("backend", "image")
            .WithEnvironment("FIRST", firstBrowser)
            .WithEnvironment("SECOND", secondBrowser)
            .WithEnvironment("REPEATED", ReferenceExpression.Create($"{firstBrowser}|{secondBrowser}|{firstBrowser}"));
        using var app = builder.Build();

        var manifest = await ManifestUtils.GetManifestForModel(app.Services.GetRequiredService<DistributedApplicationModel>());

        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
        await Verify(manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json");
    }

    [Fact]
    public void ManifestBrowserInstanceNamesAreBoundedStableAndThreadSafe()
    {
        var resource = new ContainerResource(new string('a', 64));
        var expression = ReferenceExpression.Create($"https://example");
        var values = Enumerable.Range(0, 24).Select(_ =>
            new BrowserOriginReference(resource).WithPublishExpression(expression)).ToArray();
        Parallel.ForEach(values, value =>
        {
            var name = value.ManifestResourceName;
            Assert.InRange(name.Length, 1, ModelName.DefaultMaxLength);
            Assert.Equal(name, value.ManifestResourceName);
            Assert.Equal($"{{{name}.connectionString}}", value.ValueExpression);
        });

        Assert.Equal(values.Length, values.Select(value => value.ManifestResourceName).Distinct().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManifestRejectsUnsupportedBrowserProjection(bool interpolate)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var frontend = builder.AddContainer("frontend", "image").WithHttpEndpoint(targetPort: 8080);
        var origin = frontend.GetBrowserOrigin();
        builder.AddContainer("backend", "image")
            .WithEnvironment("ORIGIN", interpolate ? ReferenceExpression.Create($"{origin}") : origin);
        using var app = builder.Build();

        await Assert.ThrowsAsync<NotSupportedException>(async () => await ManifestUtils.GetManifestForModel(
            app.Services.GetRequiredService<DistributedApplicationModel>()));
    }

    [Fact]
    public async Task JoinSupportsEmptyAndLongSeparators()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointReference(resource, "http");
        var reference = endpoint.GetBrowserOrigin();
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
        [
            new() { Endpoint = endpoint, Url = "http://first.example/" },
            new() { Endpoint = endpoint, Url = "http://second.example/" }
        ]);
        var separator = new string('-', 80);

        Assert.Equal("http://first.examplehttp://second.example", await reference.Origins.Join("").GetValueAsync());
        Assert.Equal($"http://first.example{separator}http://second.example", await reference.Origins.Join(separator).GetValueAsync());
    }

    [Theory]
    [InlineData("https://b\u00fccher.example/app?x=1#top", "https://xn--bcher-kva.example", "xn--bcher-kva.example", 443)]
    [InlineData("HTTPS://XN--BCHER-KVA.EXAMPLE:443/app", "https://xn--bcher-kva.example", "xn--bcher-kva.example", 443)]
    [InlineData("https://b\u00fccher.example:9443/app", "https://xn--bcher-kva.example:9443", "xn--bcher-kva.example", 9443)]
    [InlineData("https://[::1]:443/app", "https://[::1]", "[::1]", 443)]
    [InlineData("http://[::1]:8081/app", "http://[::1]:8081", "[::1]", 8081)]
    [InlineData("http://127.0.0.1:80/app", "http://127.0.0.1", "127.0.0.1", 80)]
    [InlineData("https://reader@frontend.example/app?x=1#top", "https://frontend.example", "frontend.example", 443)]
    public async Task NormalizesBrowserOriginsWithoutChangingAllocations(string url, string expectedOrigin, string expectedHost, int expectedPort)
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpoint);
        endpoint.AllocatedEndpoint = new(endpoint, "localhost", 8080);
        var reference = resource.GetEndpoint("http").GetBrowserOrigin();
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
            [new ResourceUrlAnnotation { Endpoint = resource.GetEndpoint("http"), Url = url }]);

        Assert.Equal(expectedOrigin, await reference.GetValueAsync());
        var browserUrl = Assert.Single(await reference.GetUrlsAsync());
        Assert.Equal(expectedHost, browserUrl.Url.Host);
        Assert.Equal(expectedPort, browserUrl.Url.Port);
        Assert.Equal("http://localhost:8080", resource.GetEndpoint("http").Url);
    }

    [Fact]
    public async Task WaitsForFinalizedUrlsRatherThanPhysicalAllocation()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpoint);
        endpoint.AllocatedEndpoint = new(endpoint, "localhost", 8080);
        var reference = resource.GetEndpoint("http").GetBrowserOrigin();
        var origin = reference.GetValueAsync();
        Assert.False(origin.IsCompleted);

        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
            [new ResourceUrlAnnotation { Endpoint = resource.GetEndpoint("http"), Url = "https://frontend.example/swagger?x=1#top" }]);

        Assert.Equal("https://frontend.example", await origin.DefaultTimeout());
        Assert.Equal("https://frontend.example/swagger?x=1#top", await reference.PreferredUrl.GetValueAsync());
        Assert.Equal("https://frontend.example/signin-oidc",
            await ReferenceExpression.Create($"{reference}/signin-oidc").GetValueAsync(CancellationToken.None));
        var resolved = await ExpressionResolver.ResolveAsync(reference, new ValueProviderContext
        {
            Caller = new ContainerResource("consumer"),
            Network = KnownNetworkIdentifiers.DefaultAspireContainerNetwork
        }, CancellationToken.None);
        Assert.Equal("https://frontend.example", resolved.Value);
    }

    [Fact]
    public async Task OriginCollectionsDeduplicateNavigationPathsAndPreserveDistinctAddresses()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointReference(resource, "https");
        var reference = endpoint.GetBrowserOrigin();
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
        [
            new() { Endpoint = endpoint, Url = "https://frontend.example/app" },
            new() { Endpoint = endpoint, Url = "https://frontend.example:443/swagger" },
            new() { Endpoint = endpoint, Url = "http://preferred.example:8080/app", DisplayOrder = 10 },
            new() { Endpoint = endpoint, Url = "https://127.0.0.1:8443/" },
            new() { Endpoint = endpoint, Url = "https://frontend.example:9443/" },
            new() { Endpoint = endpoint, Url = "https://localhost:8443/", DisplayLocation = UrlDisplayLocation.DetailsOnly }
        ]);

        Assert.Equal("http://preferred.example:8080", await reference.GetValueAsync());
        Assert.Equal(
            ["http://preferred.example:8080", "https://frontend.example", "https://127.0.0.1:8443", "https://frontend.example:9443", "https://localhost:8443"],
            await reference.Origins.GetValuesAsync());
        Assert.Equal(
            "http://preferred.example:8080;https://frontend.example;https://127.0.0.1:8443;https://frontend.example:9443;https://localhost:8443",
            await reference.Origins.Join(";").GetValueAsync());
        var jsonValues = JsonSerializer.Deserialize<string[]>((await reference.Origins.AsJson().GetValueAsync())!);
        Assert.NotNull(jsonValues);
        Assert.Equal(
            ["http://preferred.example:8080", "https://frontend.example", "https://127.0.0.1:8443", "https://frontend.example:9443", "https://localhost:8443"],
            jsonValues);
        Assert.Equal(6, (await reference.Urls.GetValuesAsync()).Count);
    }

    [Fact]
    public async Task CustomConsumerCallbackCanExpandOriginsIntoRedirectUris()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointReference(resource, "https");
        var reference = endpoint.GetBrowserOrigin();
        var caller = new ContainerResource("consumer");
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
        [
            new() { Endpoint = endpoint, Url = "https://frontend.example/swagger" },
            new() { Endpoint = endpoint, Url = "https://tunnel.example/" }
        ]);
        var value = reference.CreateValue("redirect-uris", context =>
        {
            Assert.Same(caller, context.ValueProviderContext.Caller);
            return ValueTask.FromResult<string?>(JsonSerializer.Serialize(context.Urls
                .SelectMany(url => new[] { $"{url.Origin}/signin-oidc", $"{url.Origin}/signout-callback-oidc" })));
        });
        var expression = ReferenceExpression.Create($"redirects={value}");

        var resolved = await expression.GetValueAsync(new ValueProviderContext { Caller = caller }, CancellationToken.None);

        Assert.Equal(
            "redirects=[\"https://frontend.example/signin-oidc\",\"https://frontend.example/signout-callback-oidc\",\"https://tunnel.example/signin-oidc\",\"https://tunnel.example/signout-callback-oidc\"]",
            resolved);
        Assert.Same(resource, Assert.Single(((IValueWithReferences)value).References));
        Assert.Same(resource, Assert.Single(((IValueWithReferences)reference).References));
    }

    [Fact]
    public async Task PublishingUsesExplicitExpressionWithoutResolvingLocalUrls()
    {
        var resource = new ContainerResource("frontend");
        var reference = new BrowserOriginReference(resource, "https");
        var publicHost = new ParameterResource("public-host", _ => "public.example");
        var published = ReferenceExpression.Create($"https://{publicHost}");
        var value = reference.WithPublishExpression(published);
        var context = new ValueProviderContext
        {
            ExecutionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish)
        };

        Assert.Equal("https://public.example", await value.GetValueAsync(context, CancellationToken.None));
        Assert.Same(published, value.ToReferenceExpression(context.ExecutionContext));
        Assert.Equal([resource, published], ((IValueWithReferences)value).References);
        Assert.False(BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Value.IsValueSet);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await reference.GetValueAsync(context, CancellationToken.None));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await reference.GetUrlsAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task OutputConfigurationsShareOneProviderTypeAndRemainIndependent()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointReference(resource, "https");
        var origin = endpoint.GetBrowserOrigin();
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
        [
            new() { Endpoint = endpoint, Url = "https://frontend.example/app" },
            new() { Endpoint = endpoint, Url = "https://tunnel.example/" }
        ]);
        var publishingExpression = ReferenceExpression.Create($"https://published.example");
        var publishContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var fullUrl = origin.PreferredUrl.WithPublishExpression(publishingExpression);
        var joined = origin.Origins.Join(";").WithPublishExpression(publishingExpression);
        var json = origin.Origins.AsJson();
        var custom = origin.CreateValue("url-count", context =>
            ValueTask.FromResult<string?>(context.Urls.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        BrowserOriginReference[] values = [origin, fullUrl, joined, json, custom];
        Assert.All(values, value => Assert.IsType<BrowserOriginReference>(value));
        Assert.Equal("https://frontend.example", await origin.GetValueAsync());
        Assert.Equal("https://frontend.example/app", await fullUrl.GetValueAsync());
        Assert.Equal("https://frontend.example;https://tunnel.example", await joined.GetValueAsync());
        Assert.Equal("[\"https://frontend.example\",\"https://tunnel.example\"]", await json.GetValueAsync());
        Assert.Equal("2", await custom.GetValueAsync());
        Assert.Same(publishingExpression, fullUrl.ToReferenceExpression(publishContext));
        Assert.Same(publishingExpression, joined.ToReferenceExpression(publishContext));
        Assert.Throws<NotSupportedException>(() => origin.ToReferenceExpression(publishContext));
        Assert.Equal(["https://frontend.example", "https://tunnel.example"], await fullUrl.Origins.GetValuesAsync());

        var callback = ReferenceExpression.Create($"{origin}/signin-oidc");
        Assert.Same(origin, Assert.Single(callback.ValueProviders));
        Assert.Equal("https://frontend.example/signin-oidc", await callback.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReferenceExpressionConversionSeparatesRuntimeAndDeploymentTrees()
    {
        var resource = new ContainerResource("frontend");
        var endpoint = new EndpointReference(resource, "https");
        var publicHost = new ParameterResource("public-host", _ => "public.example");
        var published = ReferenceExpression.Create($"https://{publicHost}");
        var origin = endpoint.GetBrowserOrigin().WithPublishExpression(published);
        var runtime = origin.ToReferenceExpression(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        var deployment = origin.ToReferenceExpression(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish));

        Assert.Same(origin, Assert.Single(runtime.ValueProviders));
        Assert.Same(published, deployment);
        Assert.Same(publicHost, Assert.Single(deployment.ValueProviders));
        Assert.False(BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Value.IsValueSet);
        Assert.Equal("https://public.example", await deployment.GetValueAsync(CancellationToken.None));

        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
            [new ResourceUrlAnnotation { Endpoint = endpoint, Url = "https://local.example/app" }]);
        Assert.Equal("https://local.example", await runtime.GetValueAsync(CancellationToken.None));
        Assert.Throws<ArgumentNullException>("executionContext", () => origin.ToReferenceExpression(null!));
    }

    [Fact]
    public async Task CancellationStopsWaitingForBrowserUrls()
    {
        var reference = new BrowserOriginReference(new ContainerResource("frontend"));
        using var cts = new CancellationTokenSource();
        var value = reference.Origins.Join(";").GetValueAsync(cts.Token);
        Assert.False(value.IsCompleted);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await value);
    }

    [Fact]
    public async Task EndpointFilterAndHttpsPreferenceUseFinalizedMetadata()
    {
        var resource = new ContainerResource("frontend");
        BrowserUrlSnapshotAnnotation.GetOrCreate(resource).Capture(resource,
        [
            new() { Endpoint = new EndpointReference(resource, "http"), Url = "http://frontend.example/" },
            new() { Endpoint = new EndpointReference(resource, "https"), Url = "https://frontend.example/" }
        ]);

        Assert.Equal("https://frontend.example", await new BrowserOriginReference(resource).GetValueAsync());
        Assert.Equal("http://frontend.example", await new BrowserOriginReference(resource, "HTTP").GetValueAsync());
    }
}
