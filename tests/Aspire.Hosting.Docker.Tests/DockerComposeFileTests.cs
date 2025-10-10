// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposeFileTests(ITestOutputHelper output)
{
    [Fact]
    public void AddDockerComposeFile_ParsesSimpleService()
    {
        // Create a temp docker-compose.yml file
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-file-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        
        var composeFilePath = Path.Combine(tempDir.FullName, "docker-compose.yml");
        File.WriteAllText(composeFilePath, @"
version: '3.8'
services:
  redis:
    image: redis:7.0
    ports:
      - ""6379:6379""
");

        try
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            
            // Add the docker compose file
            var composeResource = builder.AddDockerComposeFile("mycompose", composeFilePath);
            
            // Verify the compose resource was created
            Assert.NotNull(composeResource);
            Assert.Equal("mycompose", composeResource.Resource.Name);
            Assert.Equal(composeFilePath, composeResource.Resource.ComposeFilePath);
            
            // Build the app to ensure resources are registered
            var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            
            // Verify that a redis container was created
            var redisResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "redis");
            Assert.NotNull(redisResource);
            
            // Verify the image was set correctly
            var imageAnnotation = redisResource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
            Assert.NotNull(imageAnnotation);
            Assert.Equal("redis", imageAnnotation.Image);
            Assert.Equal("7.0", imageAnnotation.Tag);
            
            // Verify the endpoint was created
            var endpoints = redisResource.Annotations.OfType<EndpointAnnotation>();
            Assert.NotEmpty(endpoints);
            var endpoint = endpoints.First();
            Assert.Equal(6379, endpoint.Port);
            Assert.Equal(6379, endpoint.TargetPort);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
    
    [Fact]
    public void AddDockerComposeFile_ParsesMultipleServices()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-file-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        
        var composeFilePath = Path.Combine(tempDir.FullName, "docker-compose.yml");
        File.WriteAllText(composeFilePath, @"
version: '3.8'
services:
  web:
    image: nginx:latest
    ports:
      - ""8080:80""
  db:
    image: postgres:14
    environment:
      POSTGRES_PASSWORD: secret
");

        try
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            
            builder.AddDockerComposeFile("mycompose", composeFilePath);
            
            var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            
            // Verify web service
            var webResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "web");
            Assert.NotNull(webResource);
            
            // Verify db service
            var dbResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "db");
            Assert.NotNull(dbResource);
            
            // Verify environment variable was set on db
            var envAnnotations = dbResource.Annotations.OfType<EnvironmentCallbackAnnotation>();
            Assert.NotEmpty(envAnnotations);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
    
    [Fact]
    public void AddDockerComposeFile_ThrowsWhenFileNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var exception = Assert.Throws<FileNotFoundException>(() =>
        {
            builder.AddDockerComposeFile("mycompose", "/nonexistent/docker-compose.yml");
        });
        
        Assert.Contains("Docker Compose file not found", exception.Message);
    }
    
    [Fact]
    public void AddDockerComposeFile_SkipsServicesWithoutImage()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-file-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        
        var composeFilePath = Path.Combine(tempDir.FullName, "docker-compose.yml");
        File.WriteAllText(composeFilePath, @"
version: '3.8'
services:
  app:
    build:
      context: .
    ports:
      - ""3000:3000""
  cache:
    image: redis:latest
");

        try
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            
            builder.AddDockerComposeFile("mycompose", composeFilePath);
            
            var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            
            // Verify that only cache service was created (app has build, no image)
            var cacheResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "cache");
            Assert.NotNull(cacheResource);
            
            var appResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "app");
            Assert.Null(appResource);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
    
    [Fact]
    public void AddDockerComposeFile_ParsesVolumeMounts()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-file-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        
        var composeFilePath = Path.Combine(tempDir.FullName, "docker-compose.yml");
        File.WriteAllText(composeFilePath, @"
version: '3.8'
services:
  app:
    image: myapp:latest
    volumes:
      - type: bind
        source: ./data
        target: /app/data
        read_only: true
      - type: volume
        source: appdata
        target: /var/lib/app
");

        try
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            
            builder.AddDockerComposeFile("mycompose", composeFilePath);
            
            var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            
            var appResource = appModel.Resources.OfType<ContainerResource>()
                .FirstOrDefault(r => r.Name == "app");
            Assert.NotNull(appResource);
            
            var mounts = appResource.Annotations.OfType<ContainerMountAnnotation>();
            Assert.NotEmpty(mounts);
            Assert.Equal(2, mounts.Count());
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
