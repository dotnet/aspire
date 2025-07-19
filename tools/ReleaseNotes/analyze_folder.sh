#!/bin/bash
# Comprehensive folder analysis script for release notes generation

FOLDER=$1
BASE_BRANCH=${2:-origin/release/9.3}
TARGET_BRANCH=${3:-origin/release/9.4}

if [ -z "$FOLDER" ]; then
    echo "Usage: $0 <folder_path> [base_branch] [target_branch]"
    echo "Example: $0 src/Aspire.Cli origin/release/9.3 origin/release/9.4"
    echo ""
    echo "Environment variables can also be used:"
    echo "  BASE_BRANCH=origin/release/9.3 TARGET_BRANCH=origin/release/9.4 $0 src/Aspire.Cli"
    exit 1
fi

# Use environment variables if set, otherwise use parameters
BASE_BRANCH=${BASE_BRANCH:-${2:-origin/release/9.3}}
TARGET_BRANCH=${TARGET_BRANCH:-${3:-origin/release/9.4}}

# Ensure we're in the git repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

echo "📁 ANALYZING: $FOLDER"
echo "🔄 Comparing: $BASE_BRANCH → $TARGET_BRANCH"
echo "📂 Working from: $(pwd)"
echo "⏱️  Starting detailed analysis..."
echo "========================================"

# Start timing
ANALYSIS_START_TIME=$(date +%s)

echo "📊 Change Summary:"
STATS=$(git diff --stat $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/)
if [ -n "$STATS" ]; then
    echo "$STATS" | tail -1
else
    echo "No changes found in this folder"
    exit 0
fi

echo -e "\n All Commits:"
git log --oneline --no-merges $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/

echo -e "\n👥 Top Contributors:"
git log --format="%an" $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/ | sort | uniq -c | sort -nr | head -5

echo -e "\n📝 Sample Commit Messages (categorized):"
echo "Feature commits:"
git log --grep="feat\|feature\|add" --oneline --no-merges $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/ | head -5

echo "Bug fixes:"
git log --grep="fix\|bug" --oneline --no-merges $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/ | head -5

echo "Breaking changes:"
git log --grep="breaking\|BREAKING" --oneline --no-merges $BASE_BRANCH..$TARGET_BRANCH -- $FOLDER/ | head -5

# Calculate and display timing
ANALYSIS_END_TIME=$(date +%s)
TOTAL_TIME=$((ANALYSIS_END_TIME - ANALYSIS_START_TIME))

echo "========================================"
echo "⏱️  Analysis completed in ${TOTAL_TIME}s"
echo "📁 Analysis for: $FOLDER"
echo "🔄 Branch comparison: $BASE_BRANCH → $TARGET_BRANCH"
echo "========================================"
echo "✅ Analysis complete for $FOLDER"
echo "📊 Comparison: $BASE_BRANCH → $TARGET_BRANCH"
echo "Use the data above to generate release notes for this component"
