#!/usr/bin/env python3

import os
import tempfile
import shutil
import subprocess

# Create a test directory structure
test_dir = tempfile.mkdtemp()
print(f"Test directory: {test_dir}")

# Create source files for bind mount
source_dir = os.path.join(test_dir, "source")
os.makedirs(source_dir)

# Create some test files
with open(os.path.join(source_dir, "config.json"), "w") as f:
    f.write('{"key": "value"}')

with open(os.path.join(source_dir, "script.sh"), "w") as f:
    f.write('#!/bin/bash\necho "Hello World"')

# Create output directory
output_dir = os.path.join(test_dir, "output")
os.makedirs(output_dir)

print(f"Source directory: {source_dir}")
print(f"Output directory: {output_dir}")
print(f"Files in source: {os.listdir(source_dir)}")

# Create a minimal aspire app with bind mount
app_dir = os.path.join(test_dir, "app")
os.makedirs(app_dir)

program_cs = f"""
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");

builder.AddContainer("test-container", "busybox")
    .WithBindMount("{source_dir}", "/app/config");

var app = builder.Build();
app.Run();
"""

with open(os.path.join(app_dir, "Program.cs"), "w") as f:
    f.write(program_cs)

project_file = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="*" />
  </ItemGroup>
</Project>
"""

with open(os.path.join(app_dir, "TestApp.csproj"), "w") as f:
    f.write(project_file)

print(f"Created test app in: {app_dir}")
print("This script demonstrates the current behavior where bind mounts are NOT copied to output folder")