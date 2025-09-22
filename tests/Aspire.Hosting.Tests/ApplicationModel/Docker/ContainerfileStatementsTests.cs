// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.ApplicationModel.Docker;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class ContainerfileStatementsTests
{
    [Fact]
    public async Task FromStatement_WithoutStage_WritesCorrectFormat()
    {
        // Arrange
        var statement = new FromStatement("node:20-bullseye");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("FROM node:20-bullseye\n", result);
    }

    [Fact]
    public async Task FromStatement_WithStage_WritesCorrectFormat()
    {
        // Arrange
        var statement = new FromStatement("node:20-bullseye", "builder");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("FROM node:20-bullseye AS builder\n", result);
    }

    [Fact]
    public async Task WorkDirStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new WorkDirStatement("/app");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("WORKDIR /app\n", result);
    }

    [Fact]
    public async Task RunStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new RunStatement("npm install");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("RUN npm install\n", result);
    }

    [Fact]
    public async Task RunStatement_WithComplexCommand_WritesCorrectFormat()
    {
        // Arrange
        var command = "apt-get update && apt-get install -y --no-install-recommends git ca-certificates && rm -rf /var/lib/apt/lists/*";
        var statement = new RunStatement(command);
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"RUN {command}\n", result);
    }

    [Fact]
    public async Task CopyStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new CopyStatement("package*.json", "./");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("COPY package*.json ./\n", result);
    }

    [Fact]
    public async Task CopyFromStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new CopyFromStatement("builder", "/app/dist", "/srv");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("COPY --from=builder /app/dist /srv\n", result);
    }

    [Fact]
    public async Task EnvStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new EnvStatement("NODE_ENV", "production");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("ENV NODE_ENV=production\n", result);
    }

    [Fact]
    public async Task EnvStatement_WithEmptyValue_WritesCorrectFormat()
    {
        // Arrange
        var statement = new EnvStatement("PATH", "");
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("ENV PATH=\n", result);
    }

    [Fact]
    public async Task ExposeStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new ExposeStatement(3000);
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("EXPOSE 3000\n", result);
    }

    [Fact]
    public async Task CmdStatement_WritesCorrectFormat()
    {
        // Arrange
        var statement = new CmdStatement(["node", "server.js"]);
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("""CMD ["node","server.js"]""" + "\n", result);
    }

    [Fact]
    public async Task CmdStatement_WithSingleCommand_WritesCorrectFormat()
    {
        // Arrange
        var statement = new CmdStatement(["nginx"]);
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("""CMD ["nginx"]""" + "\n", result);
    }

    [Fact]
    public async Task CmdStatement_WithComplexCommand_WritesCorrectFormat()
    {
        // Arrange
        var statement = new CmdStatement(["cmd", "run", "--config", "/etc/caddy/caddy.json"]);
        using var stream = new MemoryStream();

        // Act
        await statement.WriteStatementAsync(stream);

        // Assert
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("""CMD ["cmd","run","--config","/etc/caddy/caddy.json"]""" + "\n", result);
    }
}