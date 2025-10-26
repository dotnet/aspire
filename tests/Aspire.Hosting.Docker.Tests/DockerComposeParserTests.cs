// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker.Tests;

/// <summary>
/// Tests for DockerComposeParser that verify parsing of various Docker Compose specification formats.
/// Based on the Compose Specification: https://github.com/compose-spec/compose-spec/blob/master/spec.md
/// </summary>
public class DockerComposeParserTests
{
    [Fact]
    public void ParseComposeFile_EmptyYaml_ReturnsEmptyDictionary()
    {
        var yaml = "";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseComposeFile_NoServices_ReturnsEmptyDictionary()
    {
        var yaml = @"
version: '3.8'
networks:
  mynetwork:
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseComposeFile_SingleServiceWithImage_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx:alpine
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result);
        Assert.True(result.ContainsKey("web"));
        Assert.Equal("nginx:alpine", result["web"].Image);
    }

    #region Environment Variables Tests
    // Spec: Environment variables can be defined using a dictionary or an array

    [Fact]
    public void ParseEnvironment_DictionaryFormat_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    environment:
      DEBUG: 'true'
      LOG_LEVEL: info
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["app"].Environment.Count);
        Assert.Equal("true", result["app"].Environment["DEBUG"]);
        Assert.Equal("info", result["app"].Environment["LOG_LEVEL"]);
    }

    [Fact]
    public void ParseEnvironment_ArrayFormat_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    environment:
      - DEBUG=true
      - LOG_LEVEL=info
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["app"].Environment.Count);
        Assert.Equal("true", result["app"].Environment["DEBUG"]);
        Assert.Equal("info", result["app"].Environment["LOG_LEVEL"]);
    }

    [Fact]
    public void ParseEnvironment_ArrayFormatWithoutValue_SetsEmptyString()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    environment:
      - DEBUG
      - LOG_LEVEL=info
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["app"].Environment.Count);
        Assert.Equal(string.Empty, result["app"].Environment["DEBUG"]);
        Assert.Equal("info", result["app"].Environment["LOG_LEVEL"]);
    }

    #endregion

    #region Port Mapping Tests
    // Spec: Ports can be defined using short syntax (string) or long syntax (mapping)

    [Fact]
    public void ParsePorts_ShortSyntax_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - ""8080:80""
      - ""443:443/tcp""
      - ""53:53/udp""
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(3, result["web"].Ports.Count);
        
        // First port: "8080:80" - no protocol specified
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Equal(8080, result["web"].Ports[0].Published);
        Assert.Null(result["web"].Ports[0].Protocol); // Not explicitly specified
        
        // Second port: "443:443/tcp" - tcp explicitly specified
        Assert.Equal(443, result["web"].Ports[1].Target);
        Assert.Equal(443, result["web"].Ports[1].Published);
        Assert.Equal("tcp", result["web"].Ports[1].Protocol);
        
        // Third port: "53:53/udp" - udp explicitly specified
        Assert.Equal(53, result["web"].Ports[2].Target);
        Assert.Equal(53, result["web"].Ports[2].Published);
        Assert.Equal("udp", result["web"].Ports[2].Protocol);
    }

    [Fact]
    public void ParsePorts_ShortSyntaxWithHostIp_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - ""127.0.0.1:8080:80""
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["web"].Ports);
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Equal(8080, result["web"].Ports[0].Published);
        Assert.Equal("127.0.0.1", result["web"].Ports[0].HostIp);
        Assert.Null(result["web"].Ports[0].Protocol); // Not explicitly specified
    }

    [Fact]
    public void ParsePorts_LongSyntax_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - target: 80
        published: 8080
        protocol: tcp
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["web"].Ports);
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Equal(8080, result["web"].Ports[0].Published);
        Assert.Equal("tcp", result["web"].Ports[0].Protocol);
    }

    [Fact]
    public void ParsePorts_LongSyntaxWithUdp_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  dns:
    image: dns-server
    ports:
      - target: 53
        published: 5353
        protocol: udp
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["dns"].Ports);
        Assert.Equal(53, result["dns"].Ports[0].Target);
        Assert.Equal(5353, result["dns"].Ports[0].Published);
        Assert.Equal("udp", result["dns"].Ports[0].Protocol);
    }

    [Fact]
    public void ParsePorts_LongSyntaxWithHostIp_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - target: 80
        published: 8080
        host_ip: 127.0.0.1
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["web"].Ports);
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Equal(8080, result["web"].Ports[0].Published);
        Assert.Equal("127.0.0.1", result["web"].Ports[0].HostIp);
        Assert.Null(result["web"].Ports[0].Protocol); // Protocol not explicitly specified in long syntax
    }

    [Fact]
    public void ParsePorts_ContainerPortOnly_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - ""80""
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["web"].Ports);
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Null(result["web"].Ports[0].Published); // Not specified, will be randomly assigned
        Assert.Null(result["web"].Ports[0].Protocol); // Not explicitly specified
    }

    [Fact]
    public void ParsePorts_LongSyntaxWithName_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    ports:
      - name: web
        target: 80
        published: 8080
      - name: web-secured
        target: 443
        published: 8443
        protocol: tcp
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["web"].Ports.Count);
        
        // First port with name
        Assert.Equal(80, result["web"].Ports[0].Target);
        Assert.Equal(8080, result["web"].Ports[0].Published);
        Assert.Equal("web", result["web"].Ports[0].Name);
        
        // Second port with name
        Assert.Equal(443, result["web"].Ports[1].Target);
        Assert.Equal(8443, result["web"].Ports[1].Published);
        Assert.Equal("web-secured", result["web"].Ports[1].Name);
    }

    #endregion

    #region Volume Tests
    // Spec: Volumes can be defined using short syntax (string) or long syntax (mapping)

    [Fact]
    public void ParseVolumes_ShortSyntaxBindMount_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    volumes:
      - ./data:/app/data
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Volumes);
        var volume = result["app"].Volumes[0];
        Assert.Equal("./data", volume.Source);
        Assert.Equal("/app/data", volume.Target);
        Assert.Equal("bind", volume.Type);
        Assert.False(volume.ReadOnly);
    }

    [Fact]
    public void ParseVolumes_ShortSyntaxReadOnly_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    volumes:
      - ./config:/app/config:ro
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Volumes);
        var volume = result["app"].Volumes[0];
        Assert.Equal("./config", volume.Source);
        Assert.Equal("/app/config", volume.Target);
        Assert.True(volume.ReadOnly);
    }

    [Fact]
    public void ParseVolumes_ShortSyntaxNamedVolume_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  db:
    image: postgres
    volumes:
      - dbdata:/var/lib/postgresql/data
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["db"].Volumes);
        var volume = result["db"].Volumes[0];
        Assert.Equal("dbdata", volume.Source);
        Assert.Equal("/var/lib/postgresql/data", volume.Target);
        Assert.Equal("volume", volume.Type);
    }

    [Fact]
    public void ParseVolumes_ShortSyntaxAnonymous_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    volumes:
      - /app/node_modules
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Volumes);
        var volume = result["app"].Volumes[0];
        Assert.Null(volume.Source);
        Assert.Equal("/app/node_modules", volume.Target);
        Assert.Equal("volume", volume.Type);
    }

    [Fact]
    public void ParseVolumes_LongSyntax_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    volumes:
      - type: bind
        source: ./data
        target: /app/data
        read_only: true
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Volumes);
        var volume = result["app"].Volumes[0];
        Assert.Equal("bind", volume.Type);
        Assert.Equal("./data", volume.Source);
        Assert.Equal("/app/data", volume.Target);
        Assert.True(volume.ReadOnly);
    }

    #endregion

    #region Build Configuration Tests
    // Spec: Build can be a string (context path) or an object with additional options

    [Fact]
    public void ParseBuild_ShortSyntax_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    build: ./app
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.NotNull(result["app"].Build);
        Assert.Equal("./app", result["app"].Build!.Context);
    }

    [Fact]
    public void ParseBuild_LongSyntax_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    build:
      context: ./app
      dockerfile: Dockerfile.prod
      target: production
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        var build = result["app"].Build;
        Assert.NotNull(build);
        Assert.Equal("./app", build.Context);
        Assert.Equal("Dockerfile.prod", build.Dockerfile);
        Assert.Equal("production", build.Target);
    }

    [Fact]
    public void ParseBuild_WithArgs_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    build:
      context: .
      args:
        NODE_VERSION: '18'
        BUILD_ENV: production
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        var build = result["app"].Build;
        Assert.NotNull(build);
        Assert.Equal(2, build.Args.Count);
        Assert.Equal("18", build.Args["NODE_VERSION"]);
        Assert.Equal("production", build.Args["BUILD_ENV"]);
    }

    [Fact]
    public void ParseBuild_WithArgsArray_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    build:
      context: .
      args:
        - NODE_VERSION=18
        - BUILD_ENV=production
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        var build = result["app"].Build;
        Assert.NotNull(build);
        Assert.Equal(2, build.Args.Count);
        Assert.Equal("18", build.Args["NODE_VERSION"]);
        Assert.Equal("production", build.Args["BUILD_ENV"]);
    }

    #endregion

    #region Command and Entrypoint Tests
    // Spec: Command and entrypoint can be a string or an array

    [Fact]
    public void ParseCommand_String_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    command: bundle exec thin -p 3000
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Command);
        Assert.Equal("bundle exec thin -p 3000", result["app"].Command[0]);
    }

    [Fact]
    public void ParseCommand_Array_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    command: [""bundle"", ""exec"", ""thin"", ""-p"", ""3000""]
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(5, result["app"].Command.Count);
        Assert.Equal("bundle", result["app"].Command[0]);
        Assert.Equal("3000", result["app"].Command[4]);
    }

    [Fact]
    public void ParseEntrypoint_String_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    entrypoint: /app/start.sh
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Single(result["app"].Entrypoint);
        Assert.Equal("/app/start.sh", result["app"].Entrypoint[0]);
    }

    [Fact]
    public void ParseEntrypoint_Array_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  app:
    image: myapp
    entrypoint: [""/app/start.sh"", ""--verbose""]
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["app"].Entrypoint.Count);
        Assert.Equal("/app/start.sh", result["app"].Entrypoint[0]);
        Assert.Equal("--verbose", result["app"].Entrypoint[1]);
    }

    #endregion

    #region Dependencies Tests
    // Spec: depends_on can be an array of service names or an object with conditions

    [Fact]
    public void ParseDependsOn_SimpleArray_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    depends_on:
      - db
      - cache
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["web"].DependsOn.Count);
        Assert.True(result["web"].DependsOn.ContainsKey("db"));
        Assert.True(result["web"].DependsOn.ContainsKey("cache"));
        Assert.Equal("service_started", result["web"].DependsOn["db"].Condition);
    }

    [Fact]
    public void ParseDependsOn_WithConditions_ParsesCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx
    depends_on:
      db:
        condition: service_healthy
      cache:
        condition: service_started
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result["web"].DependsOn.Count);
        Assert.Equal("service_healthy", result["web"].DependsOn["db"].Condition);
        Assert.Equal("service_started", result["web"].DependsOn["cache"].Condition);
    }

    #endregion

    #region Multiple Services Tests

    [Fact]
    public void ParseComposeFile_MultipleServices_ParsesAllCorrectly()
    {
        var yaml = @"
version: '3.8'
services:
  web:
    image: nginx:alpine
    ports:
      - ""80:80""
  
  api:
    build: ./api
    environment:
      NODE_ENV: production
    depends_on:
      - db
  
  db:
    image: postgres:15
    volumes:
      - dbdata:/var/lib/postgresql/data
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(3, result.Count);
        
        // Verify web service
        Assert.Equal("nginx:alpine", result["web"].Image);
        Assert.Single(result["web"].Ports);
        
        // Verify api service
        Assert.NotNull(result["api"].Build);
        Assert.Equal("./api", result["api"].Build!.Context);
        Assert.Single(result["api"].Environment);
        Assert.Single(result["api"].DependsOn);
        
        // Verify db service
        Assert.Equal("postgres:15", result["db"].Image);
        Assert.Single(result["db"].Volumes);
    }

    #endregion

    #region Service Name Case Sensitivity Tests

    [Fact]
    public void ParseComposeFile_ServiceNames_AreCaseInsensitive()
    {
        var yaml = @"
version: '3.8'
services:
  WEB:
    image: nginx
  Api:
    image: node
";
        var result = DockerComposeParser.ParseComposeFile(yaml);
        
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("WEB"));
        Assert.True(result.ContainsKey("web")); // Case insensitive
        Assert.True(result.ContainsKey("Api"));
        Assert.True(result.ContainsKey("api")); // Case insensitive
    }

    #endregion
}
