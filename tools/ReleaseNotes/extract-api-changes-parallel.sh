#!/bin/bash

# Extract API changes by building current branch and using git diff
# Usage: ./extract-api-changes-parallel.sh [--core-only]

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

# Check for uncommitted changes
if ! git diff-index --quiet HEAD --; then
    echo "❌ Error: You have uncommitted changes in your working directory."
    echo "   Please commit or stash your changes before running this script."
    echo ""
    echo "📋 Uncommitted changes:"
    git status --porcelain | sed 's/^/   /'
    echo ""
    echo "🔧 To fix this:"
    echo "   git add . && git commit -m 'Save work'"
    echo "   # OR"
    echo "   git stash"
    exit 1
fi

echo "✅ Working directory is clean"

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
    if ! dotnet build "$api_diff_proj" -t:ExportProjectInfo -p:OutputPath="$API_CHANGES_DIR/" --verbosity minimal; then
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
    if dotnet build "$api_diff_proj" -t:BuildAndGenerateAPI /bl:"$API_CHANGES_DIR/build.binlog"; then
        local build_end_time=$(date +%s)
        local build_time=$((build_end_time - build_start_time))
        echo "    ✅ All projects built successfully in ${build_time}s"
    else
        local build_end_time=$(date +%s)
        local build_time=$((build_end_time - build_start_time))
        echo "    ⚠️  Some projects failed to build (${build_time}s)"
        echo "    📄 Check binary log: $API_CHANGES_DIR/build.binlog"
    fi
    
    # Now check what changed after the build using the exported API file list
    echo "🔍 Checking for API changes using exported file list..."
    
    # Read the list of expected API files
    local api_files_list="$API_CHANGES_DIR/api-files.txt"
    local api_files=()
    local generated_api_files=()
    local modified_api_files=()
    
    # Read all expected API file paths into an array
    while IFS= read -r api_file; do
        if [ -n "$api_file" ] && [ -f "$api_file" ]; then
            api_files+=("$api_file")
            
            # Check if this file was generated (exists and has content)
            if [ -s "$api_file" ]; then
                generated_api_files+=("$api_file")
                
                # Check if this file has uncommitted changes
                if ! git diff-index --quiet HEAD -- "$api_file" 2>/dev/null; then
                    modified_api_files+=("$api_file")
                fi
            fi
        fi
    done < "$api_files_list"
    
    local total_expected=${#api_files[@]}
    local total_generated=${#generated_api_files[@]}
    local total_modified=${#modified_api_files[@]}
    
    echo "    📊 API File Statistics:"
    echo "       Expected: $total_expected files"
    echo "       Generated: $total_generated files"
    echo "       Modified: $total_modified files"
    
    if [ ${#generated_api_files[@]} -eq 0 ]; then
        echo "    ℹ️  No API files were generated by the build"
        echo "No API files generated" > "$API_CHANGES_DIR/api-changes-summary.md"
        echo "No changes" > "$API_CHANGES_DIR/api-changes-diff.txt"
    else
        # Save the full diff to a file using only the generated API files
        echo "    📄 Generating diff for all generated API files..."
        git diff -- "${generated_api_files[@]}" > "$API_CHANGES_DIR/api-changes-diff.txt" 2>/dev/null || echo "# No diff available" > "$API_CHANGES_DIR/api-changes-diff.txt"
        
        echo "    📄 Found $total_generated generated API files"
        
        if [ $total_generated -gt 0 ]; then
            echo "    📝 Generated API files:"
            printf '       %s\n' "${generated_api_files[@]}"
            
            # Create uber file with all API files concatenated
            echo "    📦 Creating uber API file with all generated APIs..."
            local uber_file="$API_CHANGES_DIR/all-api-changes.txt"
            
            # Clear the uber file
            > "$uber_file"
            
            echo "# All API Changes - Uber File" >> "$uber_file"
            echo "# Generated from: $CURRENT_BRANCH (after parallel build)" >> "$uber_file"
            echo "# Generated at: $(date)" >> "$uber_file"
            echo "# Total generated API files: $total_generated" >> "$uber_file"
            echo "# Total expected API files: $total_expected" >> "$uber_file"
            echo "" >> "$uber_file"
            
            # Concatenate all generated API files
            for api_file in "${generated_api_files[@]}"; do
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
        fi
        
        # Safely revert only the generated API files to clean working directory
        echo "🔄 Reverting generated API files to clean working directory..."
        for api_file in "${generated_api_files[@]}"; do
            if [ -n "$api_file" ]; then
                if git ls-files --error-unmatch "$api_file" >/dev/null 2>&1; then
                    # File is tracked, revert it
                    git checkout HEAD -- "$api_file" 2>/dev/null || true
                else
                    # File is untracked, remove it
                    rm -f "$api_file" 2>/dev/null || true
                fi
            fi
        done
        echo "    ✅ Safely reverted $total_generated generated API files"
        
        # Create a summary report
        cat > "$API_CHANGES_DIR/api-changes-summary.md" << EOF
# API Changes Summary

Generated from: $CURRENT_BRANCH (after parallel build)
Generated at: $(date)

## Overview

This document contains API changes detected by:
1. Exporting expected API file paths from APIDiff.proj
2. Building all projects in parallel using APIDiff.proj on current branch: \`$CURRENT_BRANCH\`
3. Checking all expected API files for generation and changes

## Results

- **Expected API Files**: $total_expected
- **Generated API Files**: $total_generated
- **Modified API Files**: $total_modified

EOF

        if [ $total_generated -gt 0 ]; then
            echo "## Generated API Files" >> "$API_CHANGES_DIR/api-changes-summary.md"
            echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
            
            for api_file in "${generated_api_files[@]}"; do
                local component_name=$(echo "$api_file" | sed 's|.*/src/||' | sed 's|/api/.*||')
                echo "### $component_name" >> "$API_CHANGES_DIR/api-changes-summary.md"
                echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
                echo "**File**: \`$api_file\`" >> "$API_CHANGES_DIR/api-changes-summary.md"
                echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
                
                # Extract the diff for this specific file if it was modified
                local file_in_modified=false
                for modified_file in "${modified_api_files[@]}"; do
                    if [ "$api_file" = "$modified_file" ]; then
                        file_in_modified=true
                        break
                    fi
                done
                
                if [ "$file_in_modified" = true ]; then
                    local file_diff=$(git diff -- "$api_file")
                    if [ -n "$file_diff" ]; then
                        echo "\`\`\`diff" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        echo "$file_diff" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        echo "\`\`\`" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        
                        # Count additions and deletions
                        local additions=$(echo "$file_diff" | grep "^+" | grep -v "^+++" | wc -l)
                        local deletions=$(echo "$file_diff" | grep "^-" | grep -v "^---" | wc -l)
                        echo "- **Additions**: $additions lines" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        echo "- **Deletions**: $deletions lines" >> "$API_CHANGES_DIR/api-changes-summary.md"
                        echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
                    fi
                else
                    echo "- **Status**: New API file generated" >> "$API_CHANGES_DIR/api-changes-summary.md"
                    echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
                fi
                
                echo "---" >> "$API_CHANGES_DIR/api-changes-summary.md"
                echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
            done
        else
            echo "## No API Files Generated" >> "$API_CHANGES_DIR/api-changes-summary.md"
            echo "" >> "$API_CHANGES_DIR/api-changes-summary.md"
            echo "No API files were generated by the build process." >> "$API_CHANGES_DIR/api-changes-summary.md"
        fi
        
        echo "    ✅ API changes extracted successfully"
    fi
    
    end_time=$(date +%s)
    total_time=$((end_time - start_time))
    
    echo ""
    echo "✅ API Changes Detection completed successfully!"
    echo "   🎯 Current Branch: $CURRENT_BRANCH"
    echo "   📁 Output: $API_CHANGES_DIR/"
    echo "   📊 Expected API Files: $total_expected"
    echo "   📊 Generated API Files: $total_generated"
    echo "   📊 Modified API Files: $total_modified"
    echo "   ⏱️  Total Time: ${total_time}s"
    echo "   📄 Summary: Check $API_CHANGES_DIR/api-changes-summary.md"
    echo "   📄 Full Diff: Check $API_CHANGES_DIR/api-changes-diff.txt"
    echo "   📦 Uber API File: Check $API_CHANGES_DIR/all-api-changes.txt"
    echo "   📊 Build Log: Check $API_CHANGES_DIR/build.binlog"
    echo "   📋 Expected Files List: Check $API_CHANGES_DIR/api-files.txt"
    echo ""
}

# Run the main function
main "$@"
