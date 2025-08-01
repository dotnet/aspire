# Data Collection for Aspire Release Notes

This guide covers the complete data collection process required before generating release notes. All scripts should be run from the `tools/ReleaseNotes` directory.

## 🎯 **Goal: Comprehensive Data Foundation**

Collect all necessary data to generate accurate, comprehensive release notes by analyzing component changes and API modifications between release versions.

## 📥 **Data Collection Steps**

### **Step 1: Analyze Component Changes**

```bash
./analyze-all-components.sh <base_branch> <target_branch>
```

**Example:**
```bash
./analyze-all-components.sh release/9.4 main
```

#### **What This Generates:**

**📁 Individual Component Analysis Files** (`analysis-output/*.md`)
- One file per Aspire component (e.g., `Aspire.Hosting.md`, `Aspire.Azure.Storage.md`)
- Each file contains:
  - **Overall change statistics**: Files added/modified/deleted
  - **Complete commit history**: All commits for that component between releases
  - **Top contributors**: Who made the most changes
  - **Categorized commits**: Features, bug fixes, breaking changes

**📊 Analysis Summary** (`analysis-output/analysis-summary.md`)
- High-level overview of all component changes
- Total commit counts across all components
- Summary of major patterns and themes

#### **Expected Output Structure:**
```
analysis-output/
├── Aspire.Hosting.md                    # Core hosting functionality
├── Aspire.Hosting.Azure.md              # Azure hosting extensions
├── Aspire.Azure.Storage.md              # Azure Storage integration
├── Aspire.Cli.md                        # CLI tool changes
├── Aspire.Dashboard.md                  # Dashboard improvements
├── ... (one file per component)
└── analysis-summary.md                  # Overall summary
```

#### **Key Files to Review:**
- **`Aspire.Cli.md`** - For CLI command changes and new features
- **`Aspire.Dashboard.md`** - For dashboard UI/UX improvements
- **`Aspire.Hosting.*.md`** - For app model and hosting changes
- **`Aspire.Azure.*.md`** - For Azure integration updates

### **Step 2: Extract API Changes**

```bash
./extract-api-changes.sh
```

#### **What This Generates:**

**🔍 Uber API File** (`analysis-output/api-changes-build-current/all-api-changes.txt`)
- **Single source of truth** for all API references
- Complete API definitions from the current build
- Method signatures with exact parameter names and types
- **Critical for code sample validation** - if it's not in this file, don't document it

**📋 API Change Summary** (`analysis-output/api-changes-build-current/api-changes-summary.md`)
- New APIs added across all components
- Breaking changes and deprecations
- Method signature changes
- New extension methods and builder patterns

**🔄 Detailed API Diffs** (`analysis-output/api-changes-build-current/api-changes-diff.txt`)
- Line-by-line API differences
- Shows exactly what changed between versions
- Useful for identifying breaking changes

#### **Expected Output Structure:**
```
analysis-output/api-changes-build-current/
├── all-api-changes.txt                  # 🔑 UBER FILE - Primary API source
├── api-changes-summary.md               # Human-readable API summary
├── api-changes-diff.txt                 # Raw API differences
└── ... (additional build artifacts)
```

### **Step 3: Verify Data Collection Results**

After running both scripts, verify you have:

#### **✅ Component Analysis Verification:**
```bash
# Check component count (should be ~40+ files)
ls -1 analysis-output/*.md | wc -l

# Verify major components exist
ls analysis-output/Aspire.{Cli,Dashboard,Hosting,Azure.Storage}.md

# Check analysis summary
head -20 analysis-output/analysis-summary.md
```

#### **✅ API Changes Verification:**
```bash
# Verify uber file exists and has content
wc -l analysis-output/api-changes-build-current/all-api-changes.txt

# Check for key APIs (example)
grep -c "AddAzure" analysis-output/api-changes-build-current/all-api-changes.txt

# Review API summary
head -50 analysis-output/api-changes-build-current/api-changes-summary.md
```

## 📊 **Understanding the Output**

### **Component Analysis Files Structure**

Each component file (`*.md`) follows this structure:

```markdown
# Component Name Analysis

## Change Summary
- X files changed
- Y commits between releases
- Top contributors: [list]

## All Commits (Chronological)
[Complete list of commits with SHA and message]

## Categorized Changes
### Features
### Bug Fixes  
### Breaking Changes
```

### **Key Commit Patterns to Look For:**

- **"Add"** commits → New features or APIs
- **"Rename"** commits → Breaking changes or API updates  
- **"Improve/Enhance"** commits → Enhancements to existing features
- **"Support for"** commits → New platform/technology integrations
- **GitHub references** (`#12345`) → Look up for additional context

### **API Changes Summary Structure**

The API summary includes:

```markdown
## New APIs Added
[List of new public APIs]

## Breaking Changes
[APIs removed or changed]

## Method Signature Changes
[Parameter or return type changes]

## New Extension Methods
[New builder patterns and extensions]
```

## 🎯 **Next Steps After Data Collection**

Once data collection is complete:

1. **Review [commit-analysis.md](commit-analysis.md)** - Learn how to analyze commits for features
2. **Review [api-documentation.md](api-documentation.md)** - Understand API verification process
3. **Start feature extraction** using the collected data
4. **Generate release notes** following [writing-guidelines.md](writing-guidelines.md)

## 🚨 **Important Notes**

- **Don't run scripts during documentation writing** - Data collection should be done once upfront
- **The uber file is the single source of truth** for API verification
- **Every documented API must exist** in the uber file
- **Commit analysis provides the feature foundation** for release notes
- **GitHub issue lookup enhances** commit understanding with additional context

## 📋 **Data Collection Checklist**

- [ ] Component analysis completed (`./analyze-all-components.sh`)
- [ ] API changes extracted (`./extract-api-changes.sh`)
- [ ] Component files generated (~40+ files in `analysis-output/`)
- [ ] Uber API file created (`all-api-changes.txt`)
- [ ] API summary generated (`api-changes-summary.md`)
- [ ] Key component files verified (CLI, Dashboard, Hosting, Azure)
- [ ] Ready to proceed with feature analysis and documentation
