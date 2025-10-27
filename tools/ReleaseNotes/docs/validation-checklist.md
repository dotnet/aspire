# Validation Checklist and Success Criteria

## Success Criteria

A successful `whats-new-{version}.md` document is achieved when:

### ✅ **Comprehensive Analysis**
- [ ] ALL component analysis files have been reviewed for commit-based features
- [ ] ALL significant commits have been analyzed and translated into user-facing features
- [ ] Commit patterns have been identified and grouped into cohesive feature sections
- [ ] Multi-component features have been synthesized from related commits across components
- [ ] Stakeholder-identified commits have been analyzed and documented appropriately

### ✅ **API and CLI Accuracy**
- [ ] API changes summary has been used to identify new APIs and changes
- [ ] All code samples use APIs verified in uber file
- [ ] All CLI commands exist in commit analysis and include actual command syntax
- [ ] All API references are accurate and complete with working code samples
- [ ] No fictional features are documented

### ✅ **Breaking Changes and Migration**
- [ ] Breaking changes reflect actual API diffs and commits
- [ ] Migration guidance provided for all breaking changes
- [ ] Before/after examples included for API changes

### ✅ **Structure and Quality**
- [ ] Document follows the established template structure from `data/whats-new-*.md` files
- [ ] The reader knows where to go for more information about the features
- [ ] Consistent emoji usage and formatting
- [ ] Professional, developer-focused language

## Validation Process

Before publishing any release notes:

### 1. **Cross-reference all code samples** with the uber file

```bash
grep -n "MethodName" analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. **Verify CLI commands** against Aspire.Cli.md commit analysis

Check that all documented CLI commands appear in the commit analysis:
- `aspire exec`, `aspire deploy`, `aspire config`
- Enhanced commands: `aspire new`, `aspire add`, `aspire run`, `aspire publish`

### 3. **Check breaking changes** against actual API diffs

Verify breaking changes in:
- `analysis-output/api-changes-build-current/api-changes-diff.txt`
- Individual component analysis files

### 4. **Run markdownlint** on the generated document

```bash
npx markdownlint-cli@0.45.0 data/whats-new-{version}.md --disable MD013
```

### 5. **Validate traceability references**

For each documented change, ensure you have:
- **Commit SHA or message** from component analysis
- **GitHub Issue ID** (if referenced in commit message)
- **GitHub Pull Request number** (if available)
- **Component name** where the change was found

Example format:
```
Feature: Dashboard telemetry navigation improvements
Source: commit "Add telemetry peer navigation" in Aspire.Dashboard
GitHub PR: #10648
GitHub Issue: #10645 (if referenced)
```

### 6. **Content Quality Review**

- [ ] **Accuracy**: All APIs exist in uber file, all CLI commands exist in commit analysis
- [ ] **Completeness**: All major commits represented, no significant features missed
- [ ] **Clarity**: Developer-focused language, clear examples, actionable guidance
- [ ] **Consistency**: Follows template structure, consistent emoji usage
- [ ] **Traceability**: Can trace every documented feature back to commits/PRs

## Pre-Publication Checklist

### Final Review Items

1. **Frontmatter Validation**
   - [ ] Correct title with version number
   - [ ] Accurate description
   - [ ] Current date in ms.date field

2. **Content Structure**
   - [ ] Introduction paragraph with version info
   - [ ] Major sections with appropriate emojis
   - [ ] Breaking changes section (if applicable)
   - [ ] Proper heading hierarchy

3. **Code Examples**
   - [ ] All code blocks have language specification
   - [ ] All APIs verified in uber file
   - [ ] Complete, runnable examples where possible
   - [ ] Consistent variable naming

4. **Links and References**
   - [ ] Internal links use relative paths for Aspire docs
   - [ ] External links use xref format where appropriate
   - [ ] API references use proper xref format
   - [ ] GitHub issue/PR links included for major features

5. **CLI Documentation**
   - [ ] All commands verified in Aspire.Cli.md analysis
   - [ ] Command syntax accurate and complete
   - [ ] Examples of command output where relevant

## Common Issues to Avoid

### ❌ **API Documentation Errors**
- Documenting APIs that don't exist in the uber file
- Using wrong parameter names or types
- Mixing up `IDistributedApplicationBuilder` vs `IHostApplicationBuilder` extension methods
- Creating fluent chains that don't exist

### ❌ **CLI Documentation Errors**
- Documenting commands not found in commit analysis
- Incorrect command syntax or flags
- Feature-flagged commands without noting preview status

### ❌ **Structure Issues**
- Inconsistent emoji usage
- Missing migration guidance for breaking changes
- Poor organization (minor features before major ones)
- Missing frontmatter or incorrect YAML

### ❌ **Content Quality Issues**
- Vague or technical language instead of developer-focused
- Missing code examples for API changes
- No links to relevant documentation
- Insufficient traceability to source commits

## Post-Publication Verification

After publishing, verify:

1. **Document renders correctly** in the documentation system
2. **All links work** and point to correct destinations
3. **Code examples compile** and run successfully
4. **API references resolve** to correct documentation pages
5. **No broken internal links** or missing pages

## Quality Metrics

Track these quality indicators:

- **Coverage**: Percentage of significant commits documented
- **Accuracy**: Zero invented APIs or non-existent CLI commands
- **Traceability**: Every feature traceable to source commits/PRs
- **Completeness**: All breaking changes documented with migration paths
- **Developer Focus**: Clear, actionable examples for all major features

## Remember: Accuracy Over Completeness

Better to have fewer, accurate features that represent real commit-based improvements than many speculative ones. Every documented feature should be:

1. **Traceable** to actual commits
2. **Verifiable** through uber file or commit analysis
3. **Actionable** with working code examples
4. **Valuable** to the developer community
