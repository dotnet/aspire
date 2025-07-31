#!/usr/bin/env python3

import os
import tempfile
import subprocess
import sys

# Create a test directory structure
test_dir = tempfile.mkdtemp()
print(f"Test directory: {test_dir}")

try:
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
    <PackageReference Include="Aspire.Hosting.Docker" Version="*" />
  </ItemGroup>
</Project>
"""
    
    with open(os.path.join(app_dir, "TestApp.csproj"), "w") as f:
        f.write(project_file)
    
    print(f"Created test app in: {app_dir}")
    
    # Try to run the app to test bind mount copying
    os.chdir(app_dir)
    env = os.environ.copy()
    env['ASPIRE_ALLOW_UNSECURED_TRANSPORT'] = 'true'  # Allow unsecured transport for testing
    
    result = subprocess.run([
        "/home/runner/work/aspire/aspire/dotnet.sh", "run", 
        "--", 
        "--operation", "publish", 
        "--publisher", "default", 
        "--output-path", output_dir
    ], capture_output=True, text=True, env=env, timeout=60)
    
    print(f"Command output:")
    print(result.stdout)
    if result.stderr:
        print(f"Stderr:")
        print(result.stderr)
    
    print(f"Return code: {result.returncode}")
    
    # Check if docker-compose.yaml was created
    compose_file = os.path.join(output_dir, "docker-compose.yaml")
    if os.path.exists(compose_file):
        print(f"✓ docker-compose.yaml created")
        with open(compose_file, 'r') as f:
            content = f.read()
            print("docker-compose.yaml content:")
            print(content)
            
        # Check if bind mount files were copied
        container_dir = os.path.join(output_dir, "test-container")
        if os.path.exists(container_dir):
            print(f"✓ Container directory created: {container_dir}")
            print(f"Files in container directory: {os.listdir(container_dir)}")
            
            # Check individual files
            copied_config = os.path.join(container_dir, "config.json")
            copied_script = os.path.join(container_dir, "script.sh")
            
            if os.path.exists(copied_config):
                print(f"✓ config.json copied")
                with open(copied_config, 'r') as f:
                    print(f"config.json content: {f.read()}")
            else:
                print(f"✗ config.json not copied")
                
            if os.path.exists(copied_script):
                print(f"✓ script.sh copied")
                with open(copied_script, 'r') as f:
                    print(f"script.sh content: {f.read()}")
            else:
                print(f"✗ script.sh not copied")
        else:
            print(f"✗ Container directory not created")
    else:
        print(f"✗ docker-compose.yaml not created")
        
except Exception as e:
    print(f"Error: {e}")
    sys.exit(1)
finally:
    # Cleanup
    import shutil
    try:
        shutil.rmtree(test_dir)
        print(f"Cleaned up test directory: {test_dir}")
    except Exception as e:
        print(f"Warning: Could not cleanup {test_dir}: {e}")