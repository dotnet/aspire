# Aspire Release Notes Generation Overview

## 🎯 **Goal: Create `whats-new-{version}.md`**

The primary objective is to generate a comprehensive `whats-new-{version}.md` document that summarizes all major features, API changes, CLI enhancements, and breaking changes for the target release version, and links to relevant documentation where appropriate.

## 📥 Data Collection Steps

### 1. **Analyze Component Changes**

```bash
./analyze-all-components.sh <base_branch> <target_branch>
```

This generates:

- Individual component analysis files (`analysis-output/*.md`)
- A summary of all component changes (`analysis-output/analysis-summary.md`)
- Each file contains:
  - Overall change statistics
  - Complete commit history
  - Top contributors
  - Categorized commits (features/bugs/breaking changes)

### 2. **Extract API Changes**

```bash
./extract-api-changes.sh
```

This generates:

- **Uber API File**: `analysis-output/api-changes-build-current/all-api-changes.txt` (comprehensive API definitions)
- API change summary (`analysis-output/api-changes-build-current/api-changes-summary.md`)
- Detailed API diffs (`analysis-output/api-changes-build-current/api-changes-diff.txt`)

### 3. **Generate What's New Document**

- **GOAL**: Create `whats-new-{version}.md` for the target release
- **Use templates in `data/` directory** as structure and formatting guide (e.g., `data/whats-new-93.md`)
- **Analyze ALL files** in `analysis-output/` directory for comprehensive coverage
- **Review ALL commits** in each component analysis file to identify major features
- **Use API changes summary** from `analysis-output/api-changes-build-current/api-changes-summary.md`
- Focus on developer impact with accurate code samples for API changes
- Include CLI commands for CLI-related changes

## 🎯 COMPREHENSIVE ANALYSIS APPROACH

### 1. **ANALYZE ALL COMPONENT FILES**

Review every file in `analysis-output/` directory:

- **Individual component files** (`*.md`) contain complete commit histories
- **Look at ALL commits** in each file to identify major features and changes
- **Categorize changes** by impact: breaking changes, major features, enhancements
- **Identify patterns** across multiple components for broader themes

### 2. **API CHANGES DISCOVERY**

Use `analysis-output/api-changes-build-current/api-changes-summary.md` as the primary source for:

- **New API additions** across all components
- **Breaking changes** and deprecations
- **Method signature changes** and parameter updates
- **New extension methods** and builder patterns

### 3. **GIT COMMIT ANALYSIS FOR MISSING FEATURES**

Sometimes important features may not be immediately obvious from component analysis files. Use direct git analysis:

```bash
# Find commits with specific keywords
git log --oneline --grep="Add.*support" --since="2024-01-01"
git log --oneline --grep="container.*support" --since="2024-01-01"

# Analyze specific commits mentioned by stakeholders
git show --stat <commit-hash>
git show <commit-hash> --name-only
```

**Example: Stakeholder-Identified Commits**
When given specific commit hashes to analyze:

```bash
git show --stat bdd1d34c6  # Azure App Service container support
git show --stat d4eacc676  # Enhanced publish/deploy output
git show --stat 4ee28c24b  # Docker Compose security improvements
git show --stat 039c42594  # Azure Functions Container Apps integration
```

**Process for Stakeholder Commits:**

1. **Get commit details**: Use `git show --stat` to understand scope
2. **Look up GitHub issues**: If commit message references an issue (e.g., `#10587`), use GitHub API tools to get additional context
3. **Identify user impact**: What new capability or improvement does this enable? (Enhanced by issue context)
4. **Find related files**: Use `git show --name-only` to see what changed
5. **Create feature section**: Write user-facing documentation based on the changes and issue context
6. **Verify APIs**: Cross-reference any new APIs with the uber file

## 📋 **Mandatory Workflow for Documentation**

1. **📋 ANALYZE ALL COMPONENT FILES**: Review every `analysis-output/*.md` file for commit-based changes
   - Read through ALL commits in each component file
   - Look for patterns: "Add", "Rename", "Support", "Improve"
   - Extract user-facing impact from commit messages
   - Group related commits across components into unified features

2. **🔍 EXTRACT FEATURES FROM COMMITS**: Transform commits into user-facing documentation
   - Identify new capabilities enabled by each commit
   - Determine API changes and their impact on developers
   - Create feature sections based on commit analysis
   - Synthesize related commits into cohesive feature stories

3. **📊 CHECK API CHANGES SUMMARY**: Use `api-changes-build-current/api-changes-summary.md` for new API discoveries

4. **🔬 ANALYZE STAKEHOLDER COMMITS**: When specific commits are mentioned, deep dive with git commands
   - Use `git show --stat <commit>` to understand scope
   - Use `git show <commit> --name-only` to see affected files
   - Extract user-facing improvements from commit changes

5. **🔍 VERIFY IN UBER FILE**: Before writing ANY code sample, search the uber file for exact API definitions

6. **✅ USE ONLY CONFIRMED APIS**: If it's not in the uber file, it doesn't exist

7. **💻 PROVIDE SAMPLES FOR API CHANGES**: Every API change mentioned should include a code sample

8. **⚡ INCLUDE CLI COMMANDS**: Every CLI change should show the actual command syntax

9. **❌ NEVER INVENT APIS**: No made-up methods, parameters, or fluent chains

## **End Goal**

A professional, accurate, and comprehensive `whats-new-{version}.md` document that reflects the actual improvements made through commits, with verified APIs that developers can trust and use effectively.

**Process Summary**:

1. **Analyze commits** → Identify patterns and capabilities
2. **Extract user value** → What problems do these commits solve?
3. **Group related commits** → Create unified feature stories
4. **Verify APIs** → Use uber file for all code samples
5. **Write developer-focused** → Clear examples and migration paths

**Remember**: **Comprehensive Commit Analysis + Accuracy over completeness**

The foundation of great release notes is thorough commit analysis. Every significant commit should be considered for inclusion, translated into developer-facing language, and verified with accurate API samples. Better to have fewer, accurate features that represent real commit-based improvements than many speculative ones.
