#!/bin/bash

# Automated analysis of all components based on configuration
# Usage: ./analyze-all-components.sh <base_branch> <target_branch>

set -e

BASE_BRANCH=${1:-origin/release/9.3}
TARGET_BRANCH=${2:-origin/release/9.4}

TOOLS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="$TOOLS_DIR/config/component-priority.json"
ANALYSIS_DIR="$TOOLS_DIR/analysis-output"

echo "🔍 Starting automated component analysis"
echo "📋 Using config: $CONFIG_FILE"
echo "📊 Output directory: $ANALYSIS_DIR"
echo "⏱️  This may take several minutes for large repositories..."

# Start total timing
SCRIPT_START_TIME=$(date +%s)

# Ensure analysis directory exists
mkdir -p "$ANALYSIS_DIR"

# Check if jq is available for JSON processing
if ! command -v jq &> /dev/null; then
    echo "⚠️  jq not found, using fallback analysis"
    # Fallback to manual component list
    COMPONENTS=(
        "src"
        "extension/"
        "eng/scripts"
    )
else
    echo "📊 Processing components from configuration..."
    # Extract component paths from JSON config - flat array structure
    RAW_COMPONENTS=($(jq -r '.analysis_priorities[]' "$CONFIG_FILE" 2>/dev/null || echo ""))
    
    # Expand glob patterns to actual directories
    COMPONENTS=()
    for pattern in "${RAW_COMPONENTS[@]}"; do
        if [[ "$pattern" == *"*"* ]]; then
            # This is a glob pattern, expand it from the git root
            echo "🔍 Expanding glob pattern: $pattern"
            # Change to git root directory for proper glob expansion
            GIT_ROOT=$(git rev-parse --show-toplevel)
            cd "$GIT_ROOT"
            for expanded_path in $pattern; do
                if [ -d "$expanded_path" ]; then
                    COMPONENTS+=("$expanded_path")
                    echo "   ✅ Found: $expanded_path"
                fi
            done
            # Return to tools directory
            cd "$TOOLS_DIR"
        else
            # Regular path, add as-is if it exists
            GIT_ROOT=$(git rev-parse --show-toplevel)
            if [ -d "$GIT_ROOT/$pattern" ]; then
                COMPONENTS+=("$pattern")
                echo "   ✅ Found: $pattern"
            fi
        fi
    done
    
    # If config reading failed or no components found, use fallback
    if [ ${#COMPONENTS[@]} -eq 0 ]; then
        echo "⚠️  Could not read config or no valid components found, using fallback list"
        COMPONENTS=(
            "src"
            "extension/"
            "eng/scripts"
        )
    fi
fi

# Function to analyze a single component
analyze_component() {
    local component_start=$(date +%s)
    local component_path="$1"
    local output_file="$2"
    
    echo "  📁 Analyzing: $component_path"
    
    # Use existing analyze_folder.sh if available
    if [ -f "$TOOLS_DIR/analyze_folder.sh" ]; then
        "$TOOLS_DIR/analyze_folder.sh" "$component_path" > "$output_file"
    else
        # Fallback manual analysis
        echo "# Analysis for $component_path" > "$output_file"
        echo "" >> "$output_file"
        echo "## File Changes" >> "$output_file"
        git diff --stat $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" >> "$output_file" 2>/dev/null || echo "No changes found" >> "$output_file"
        echo "" >> "$output_file"
        echo "## All Commits" >> "$output_file"
        git log --oneline --no-merges $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" >> "$output_file" 2>/dev/null || echo "No commits found" >> "$output_file"
    fi
    
    # Add playground/test examples if available
    if [[ "$component_path" == "playground/" ]] || [[ "$component_path" == "tests/" ]]; then
        echo "" >> "$output_file"
        echo "## Notable Changes" >> "$output_file"
        git diff --name-status $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" | grep "^A" | head -10 >> "$output_file" 2>/dev/null || echo "No new files added" >> "$output_file"
    fi
    
    local component_end=$(date +%s)
    echo "    ⏱️  Completed in $((component_end - component_start))s"
}

# Function to generate safe filename from component path
generate_filename() {
    local component="$1"
    echo "$component" | sed 's|/|-|g' | sed 's|^src-||' | sed 's|-$||'
}

# Analyze all components
echo "🎯 Analyzing components..."
ANALYSIS_START=$(date +%s)

component_count=0
total_components=${#COMPONENTS[@]}
echo "📊 Processing $total_components components..."

for component in "${COMPONENTS[@]}"; do
    ((component_count++))
    echo "[$component_count/$total_components] Processing: $component"
    component_name=$(generate_filename "$component")
    output_file="$ANALYSIS_DIR/$component_name.md"
    analyze_component "$component" "$output_file"
done

ANALYSIS_END=$(date +%s)
echo "✅ Component analysis completed in $((ANALYSIS_END - ANALYSIS_START))s"

# Generate summary report
echo "📊 Generating summary report..."
SUMMARY_START=$(date +%s)
summary_file="$ANALYSIS_DIR/analysis-summary.md"

cat > "$summary_file" << EOF
# Component Analysis Summary

Generated on: $(date)
Branch comparison: $BASE_BRANCH -> $TARGET_BRANCH

## Components Analyzed

EOF

for component in "${COMPONENTS[@]}"; do
    component_name=$(generate_filename "$component")
    if [ -f "$ANALYSIS_DIR/$component_name.md" ]; then
        # Extract the actual component path from the analysis file instead of using the original pattern
        actual_component_path=$(grep "📁 ANALYZING:" "$ANALYSIS_DIR/$component_name.md" 2>/dev/null | sed 's/📁 ANALYZING: //' | tr -d '[:space:]' || echo "$component")
        if [ -n "$actual_component_path" ]; then
            # Ensure we're in the git repository root for the file count
            cd "$(git rev-parse --show-toplevel)"
            file_count=$(git diff --name-status $BASE_BRANCH..$TARGET_BRANCH -- "$actual_component_path" 2>/dev/null | wc -l | tr -d ' ')
            cd "$TOOLS_DIR"
            echo "- **$actual_component_path** ($file_count files) - [Analysis]($component_name.md)" >> "$summary_file"
        else
            echo "- **$component** (unknown files) - [Analysis]($component_name.md)" >> "$summary_file"
        fi
    fi
done

cat >> "$summary_file" << EOF

## Analysis Files Generated

EOF

ls -la "$ANALYSIS_DIR"/*.md | awk '{print "- " $9 " (" $5 " bytes)"}' >> "$summary_file"

SUMMARY_END=$(date +%s)
echo "✅ Summary generation completed in $((SUMMARY_END - SUMMARY_START))s"

# Calculate total time
SCRIPT_END_TIME=$(date +%s)
TOTAL_TIME=$((SCRIPT_END_TIME - SCRIPT_START_TIME))

echo ""
echo "✅ Component analysis complete!"
echo "⏱️  Total execution time: ${TOTAL_TIME}s"
echo ""
echo "📊 Timing Summary:"
echo "   Component Analysis: $((ANALYSIS_END - ANALYSIS_START))s"
echo "   Summary Generation: $((SUMMARY_END - SUMMARY_START))s"
echo ""
echo "📄 Summary: $summary_file"
echo "📁 Detailed analysis files in: $ANALYSIS_DIR/"
echo ""
echo "📋 Analysis files generated:"
ls -1 "$ANALYSIS_DIR"/*.md | sed 's/^/   /'
