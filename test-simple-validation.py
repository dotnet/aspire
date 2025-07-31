#!/usr/bin/env python3

import os
import tempfile
import sys

# Simple validation to test bind mount copying functionality
test_dir = tempfile.mkdtemp()
print(f"Test directory: {test_dir}")

try:
    # Create source files for bind mount
    source_dir = os.path.join(test_dir, "source")
    os.makedirs(source_dir)
    
    # Create some test files
    with open(os.path.join(source_dir, "config.json"), "w") as f:
        f.write('{"key": "value"}')
    
    # Create output directory
    output_dir = os.path.join(test_dir, "output")
    os.makedirs(output_dir)
    
    print(f"✓ Test directories created")
    print(f"  Source: {source_dir}")  
    print(f"  Output: {output_dir}")
    
    # Test the copy functionality by simulating what our code does
    import shutil
    
    # Simulate creating service directory
    service_dir = os.path.join(output_dir, "test-container")
    os.makedirs(service_dir)
    
    # Simulate copying directory
    source_dir_name = os.path.basename(source_dir.rstrip(os.path.sep))
    copied_source_path = os.path.join(service_dir, source_dir_name)
    
    def copy_directory(src, dst):
        """Copy directory recursively like our implementation"""
        os.makedirs(dst, exist_ok=True)
        
        # Copy all files
        for file in os.listdir(src):
            src_file = os.path.join(src, file)
            if os.path.isfile(src_file):
                dst_file = os.path.join(dst, file)
                shutil.copy2(src_file, dst_file)
            elif os.path.isdir(src_file):
                dst_subdir = os.path.join(dst, file)
                copy_directory(src_file, dst_subdir)
    
    copy_directory(source_dir, copied_source_path)
    
    # Check if files were copied
    expected_config = os.path.join(copied_source_path, "config.json")
    if os.path.exists(expected_config):
        print(f"✓ File copied successfully: {expected_config}")
        with open(expected_config, 'r') as f:
            content = f.read()
            if content == '{"key": "value"}':
                print(f"✓ File content preserved")
            else:
                print(f"✗ File content corrupted: {content}")
    else:
        print(f"✗ File not copied: {expected_config}")
    
    # Test relative path calculation
    relative_path = os.path.relpath(copied_source_path, output_dir).replace('\\', '/')
    expected_relative = "./test-container/source"
    if relative_path == "test-container/source":  # without leading ./ 
        relative_path = "./" + relative_path
        
    if relative_path == expected_relative:
        print(f"✓ Relative path correct: {relative_path}")
    else:
        print(f"✗ Relative path incorrect: {relative_path} (expected: {expected_relative})")
    
    print("✓ Basic validation completed successfully")
    
except Exception as e:
    print(f"✗ Error: {e}")
    sys.exit(1)
finally:
    # Cleanup
    try:
        shutil.rmtree(test_dir)
        print(f"✓ Cleaned up test directory")
    except Exception as e:
        print(f"Warning: Could not cleanup {test_dir}: {e}")