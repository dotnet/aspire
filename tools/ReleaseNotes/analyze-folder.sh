#!/bin/bash
# Comprehensive folder analysis script for release notes generation

FOLDER=$1
BASE_BRANCH=${2:-origin/release/9.4}
TARGET_BRANCH=${3:-origin/main}

if [ -z "$FOLDER" ]; then
    echo "Usage: $0 <folder_path> [base_branch] [target_branch]"
    echo "Example: $0 src/Aspire.Cli origin/release/9.4 origin/main"
    echo ""
    echo "Environment variables can also be used:"
    echo "  BASE_BRANCH=origin/release/9.4 TARGET_BRANCH=origin/main $0 src/Aspire.Cli"
    exit 1
fi

# Use environment variables if set, otherwise use parameters
# Environment variable override support
BASE_BRANCH=${BASE_BRANCH:-${2:-origin/release/9.4}}
TARGET_BRANCH=${TARGET_BRANCH:-${3:-origin/main}}

# Ensure we're in the git repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

echo "📁 ANALYZING: $FOLDER"
echo "🔄 Comparing: $BASE_BRANCH → $TARGET_BRANCH"
echo "📂 Working from: $(pwd)"
echo "⏱️  Starting detailed analysis..."
echo "🔍 Note: Only analyzing commits in $TARGET_BRANCH that are NOT in $BASE_BRANCH (excluding cherry-picks)"
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

echo -e "\n📝 All Commits (new in $TARGET_BRANCH, excluding cherry-picks):"
# Use --cherry-pick to exclude commits that were cherry-picked from base branch
git log --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- $FOLDER/

echo -e "\n👥 Top Contributors:"
git log --format="%an" --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- $FOLDER/ | sort | uniq -c | sort -nr | head -5

echo -e "\n📝 Sample Commit Messages (categorized, new commits only):"
echo "Feature commits:"
git log --grep="feat\|feature\|add" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- $FOLDER/ | head -5

echo "Bug fixes:"
git log --grep="fix\|bug" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- $FOLDER/ | head -5

echo "Breaking changes:"
git log --grep="breaking\|BREAKING" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- $FOLDER/ | head -5

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
