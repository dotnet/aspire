#!/bin/bash

# Extract API changes by building current branch and using git diff
# Usage: ./extract-api-changes.sh [--core-only]

set -e

CORE_ONLY=false

# Parse arguments
for arg in "$@"; do
    case $arg in
        --core-only)
            CORE_ONLY=true
            shift
            ;;
    esac
done

TOOLS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ANALYSIS_DIR="$TOOLS_DIR/analysis-output"
API_CHANGES_DIR="$ANALYSIS_DIR/api-changes-build-current"

echo "🔧 Extracting API changes by building current branch (parallel)"
echo "⏱️  This will build the current branch and use git diff to detect changes..."

# Start total timing
SCRIPT_START_TIME=$(date +%s)

mkdir -p "$API_CHANGES_DIR"

# Get git root
GIT_ROOT=$(git rev-parse --show-toplevel)
cd "$GIT_ROOT"

# Get current branch for reporting
CURRENT_BRANCH=$(git branch --show-current)
if [ -z "$CURRENT_BRANCH" ]; then
    CURRENT_BRANCH=$(git rev-parse HEAD)
fi

echo "💾 Current branch: $CURRENT_BRANCH"

# Main function
main() {
    start_time=$(date +%s)
    
    echo "🚀 Starting API Changes Detection Script (Current Branch Build)"
    echo "   Current Branch: $CURRENT_BRANCH"
    echo "   Core Projects Only: $CORE_ONLY"
    echo "   Output Dir: $API_CHANGES_DIR"
    echo "   Timestamp: $(date)"
    echo ""
    
    # Clean output directory
    echo "🧹 Cleaning output directory..."
    rm -rf "$API_CHANGES_DIR"
    mkdir -p "$API_CHANGES_DIR"
    echo "✅ Output directory ready: $API_CHANGES_DIR"
    
    echo " Building current branch: $CURRENT_BRANCH"
    
    # Use the existing APIDiff.proj file
    local api_diff_proj="$TOOLS_DIR/APIDiff.proj"
    
    if [ ! -f "$api_diff_proj" ]; then
        echo "❌ Error: APIDiff.proj not found at $api_diff_proj"
        echo "   Please ensure the APIDiff.proj file exists in the tools/ReleaseNotes directory"
        exit 1
    fi
    
    echo "    📝 Using existing APIDiff.proj..."
    echo "    🔨 Building all projects and generating API files..."
    
    local build_start_time=$(date +%s)
    
    # First, export the expected API file paths
    echo "    📋 Exporting project info and expected API file paths..."
    if ! "$GIT_ROOT/dotnet.sh" build "$api_diff_proj" -t:ExportProjectInfo -p:OutputPath="$API_CHANGES_DIR/" --verbosity minimal; then
        echo "    ❌ Failed to export project info"
        exit 1
    fi
    
    # Check if the API files list was generated
    local api_files_list="$API_CHANGES_DIR/api-files.txt"
    if [ ! -f "$api_files_list" ]; then
        echo "    ❌ Expected API files list not found at: $api_files_list"
        exit 1
    fi
    
    local expected_api_count=$(wc -l < "$api_files_list")
    echo "    ✅ Exported info for $expected_api_count expected API files"
    
    # Build all projects using the dedicated project file with binary logging
    echo "    📝 Binary log will be saved to: $API_CHANGES_DIR/build.binlog"
    if "$GIT_ROOT/dotnet.sh" build "$api_diff_proj" -t:BuildAndGenerateAPI /bl:"$API_CHANGES_DIR/build.binlog"; then
        local build_end_time=$(date +%s)
        local build_time=$((build_end_time - build_start_time))
        echo "    ✅ All projects built successfully in ${build_time}s"
    else
        local build_end_time=$(date +%s)
        local build_time=$((build_end_time - build_start_time))
        echo "    ⚠️  Some projects failed to build (${build_time}s)"
        echo "    📄 Check binary log: $API_CHANGES_DIR/build.binlog"
    fi
    
    # Now create uber file from all existing API files
    echo "🔍 Creating uber API file from all existing API files..."
    
    # Find all actual existing API files in the repository directly
    local existing_api_files=()
    local total_files_found=0
    
    # Find all existing .cs files in api directories
    while IFS= read -r api_file; do
        if [ -n "$api_file" ] && [ -f "$api_file" ] && [ -s "$api_file" ]; then
            existing_api_files+=("$api_file")
            ((total_files_found++))
        fi
    done < <(find "$GIT_ROOT/src" -name "*.cs" -path "*/api/*" | sort)
    
    echo "    📊 Found $total_files_found actual existing API files in the repository"
    
    if [ $total_files_found -eq 0 ]; then
        echo "    ⚠️  No existing API files found"
        echo "No API files found" > "$API_CHANGES_DIR/api-changes-summary.md"
        echo "No API files found" > "$API_CHANGES_DIR/all-api-changes.txt"
    else
        # Create uber file with all existing API files concatenated
        echo "    📦 Creating uber API file with all $total_files_found API files..."
        local uber_file="$API_CHANGES_DIR/all-api-changes.txt"
        
        # Clear the uber file
        > "$uber_file"
        
        echo "# All API Files - Uber File" >> "$uber_file"
        echo "# Generated from: $CURRENT_BRANCH" >> "$uber_file"
        echo "# Generated at: $(date)" >> "$uber_file"
        echo "# Total API files included: $total_files_found" >> "$uber_file"
        echo "" >> "$uber_file"
        
        # Concatenate all existing API files
        for api_file in "${existing_api_files[@]}"; do
            if [ -n "$api_file" ] && [ -f "$api_file" ]; then
                local component_name=$(echo "$api_file" | sed 's|.*/src/||' | sed 's|/api/.*||')
                echo "======================================" >> "$uber_file"
                echo "API FILE: $api_file" >> "$uber_file"
                echo "COMPONENT: $component_name" >> "$uber_file"
                echo "======================================" >> "$uber_file"
                echo "" >> "$uber_file"
                
                # Add the full content of the API file
                cat "$api_file" >> "$uber_file" 2>/dev/null || echo "# Error reading file $api_file" >> "$uber_file"
                echo "" >> "$uber_file"
                echo "" >> "$uber_file"
            fi
        done
        
        echo "    ✅ Uber API file created: $uber_file"
        
        # Generate git diff for all API files (both modified and newly generated)
        echo "    📄 Generating git diff for all API files..."
        
        # First get all API files that exist after build (including new ones)
        local all_api_files_after_build=()
        while IFS= read -r api_file; do
            if [ -n "$api_file" ] && [ -f "$api_file" ]; then
                all_api_files_after_build+=("$api_file")
            fi
        done < "$api_files_list"
        
        # Use git diff for the entire src directory with API file patterns to catch everything
        git diff --name-only HEAD -- 'src/*/api/*.cs' > "$API_CHANGES_DIR/changed-api-files.txt" 2>/dev/null || true
        git diff HEAD -- 'src/*/api/*.cs' > "$API_CHANGES_DIR/api-changes-diff.txt" 2>/dev/null || echo "# No tracked file changes" > "$API_CHANGES_DIR/api-changes-diff.txt"
        
        # Also capture any untracked new API files
        git ls-files --others --exclude-standard 'src/*/api/*.cs' >> "$API_CHANGES_DIR/changed-api-files.txt" 2>/dev/null || true
        
        # For new files, add their full content to the diff
        local new_files_added=0
        echo "" >> "$API_CHANGES_DIR/api-changes-diff.txt"
        echo "# ======== NEW API FILES (full content) ========" >> "$API_CHANGES_DIR/api-changes-diff.txt"
        while IFS= read -r new_file; do
            if [ -n "$new_file" ] && [ -f "$new_file" ]; then
                echo "" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                echo "diff --git a/$new_file b/$new_file" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                echo "new file mode 100644" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                echo "index 0000000..$(git hash-object "$new_file" 2>/dev/null || echo "unknown")" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                echo "--- /dev/null" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                echo "+++ b/$new_file" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                sed 's/^/+/' "$new_file" >> "$API_CHANGES_DIR/api-changes-diff.txt"
                ((new_files_added++))
            fi
        done < <(git ls-files --others --exclude-standard 'src/*/api/*.cs' 2>/dev/null || true)
        
        echo "    ✅ Git diff saved to: $API_CHANGES_DIR/api-changes-diff.txt"
        if [ $new_files_added -gt 0 ]; then
            echo "    📝 Included $new_files_added new API files in diff"
        fi
        
        # Safely revert all API files to clean working directory
        echo "🔄 Reverting all API files to clean working directory..."
        local reverted_count=0
        local removed_count=0
        
        # Revert tracked files that were modified
        while IFS= read -r api_file; do
            if [ -n "$api_file" ] && git ls-files --error-unmatch "$api_file" >/dev/null 2>&1; then
                if git checkout HEAD -- "$api_file" 2>/dev/null; then
                    ((reverted_count++))
                fi
            fi
        done < <(git diff --name-only HEAD -- 'src/*/api/*.cs' 2>/dev/null || true)
        
        # Remove untracked new API files
        while IFS= read -r new_file; do
            if [ -n "$new_file" ] && [ -f "$new_file" ]; then
                if rm -f "$new_file" 2>/dev/null; then
                    ((removed_count++))
                fi
            fi
        done < <(git ls-files --others --exclude-standard 'src/*/api/*.cs' 2>/dev/null || true)
        
        echo "    ✅ Safely reverted $reverted_count modified API files and removed $removed_count new API files"
        
        # Create a simple summary report
        cat > "$API_CHANGES_DIR/api-changes-summary.md" << EOF
# API Files Summary

Generated from: $CURRENT_BRANCH
Generated at: $(date)

## Overview

This document contains all existing API files from the Aspire repository.

## Results

- **Total API Files Included**: $total_files_found

## API Files List

EOF

        for api_file in "${existing_api_files[@]}"; do
            local component_name=$(echo "$api_file" | sed 's|.*/src/||' | sed 's|/api/.*||')
            echo "- **$component_name**: \`$api_file\`" >> "$API_CHANGES_DIR/api-changes-summary.md"
        done
        
        echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
        
        echo "    ✅ API uber file created successfully"
    fi
    
    end_time=$(date +%s)
    total_time=$((end_time - start_time))
    
    echo ""
    echo "✅ API Files Collection completed successfully!"
    echo "   🎯 Current Branch: $CURRENT_BRANCH"
    echo "   📁 Output: $API_CHANGES_DIR/"
    echo "   📊 Total API Files: $total_files_found"
    echo "   ⏱️  Total Time: ${total_time}s"
    echo "   📄 Summary: Check $API_CHANGES_DIR/api-changes-summary.md"
    echo "   📄 Git Diff: Check $API_CHANGES_DIR/api-changes-diff.txt"
    echo "   📦 Uber API File: Check $API_CHANGES_DIR/all-api-changes.txt"
    echo "   📊 Build Log: Check $API_CHANGES_DIR/build.binlog"
    echo "   📋 Expected Files List: Check $API_CHANGES_DIR/api-files.txt"
    echo ""
}

# Run the main function
main "$@"
