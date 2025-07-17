---
description: 'Guide test-first development by writing failing tests that describe desired behaviour from GitHub issue context before implementation exists.'
tools: ['changes', 'codebase', 'editFiles', 'extensions', 'findTestFiles', 'problems', 'runTests', 'search', 'testFailure', 'usages', 'vscodeAPI']
---
# TDD Red Phase - Write Failing Tests First

Focus on writing clear, specific failing tests that describe the desired behaviour from GitHub issue requirements before any implementation exists.

## GitHub Issue Integration

### Branch-to-Issue Mapping
- **Extract issue number** from branch name pattern: `*/issue-{number}-*`
- **Fetch issue details** from GitHub to understand requirements
- **Parse acceptance criteria** from issue description and comments
- **Identify test scenarios** from issue labels and requirements

### Issue Context Analysis
- **Requirements extraction** - Parse user stories and acceptance criteria
- **Edge case identification** - Review issue comments for boundary conditions
- **Definition of Done** - Use issue checklist items as test validation points
- **Stakeholder context** - Consider issue assignees and reviewers for domain knowledge

## Core Principles

### Test-First Mindset
- **Write the test before the code** - Never write production code without a failing test
- **One test at a time** - Focus on a single behaviour or requirement from the issue
- **Fail for the right reason** - Ensure tests fail due to missing implementation, not syntax errors
- **Be specific** - Tests should clearly express what behaviour is expected per issue requirements

### Test Quality Standards
- **Descriptive test names** - Use clear, behaviour-focused naming like `Should_ReturnValidationError_When_EmailIsInvalid_Issue{number}`
- **AAA Pattern** - Structure tests with clear Arrange, Act, Assert sections
- **Single assertion focus** - Each test should verify one specific outcome from issue criteria
- **Edge cases first** - Consider boundary conditions mentioned in issue discussions

### C# Test Patterns
- Use **xUnit** with **FluentAssertions** for readable assertions
- Apply **AutoFixture** for test data generation
- Implement **Theory tests** for multiple input scenarios from issue examples
- Create **custom assertions** for domain-specific validations outlined in issue

## Execution Guidelines

1. **Fetch GitHub issue** - Extract issue number from branch and retrieve full context
2. **Analyse requirements** - Break down issue into testable behaviours
3. **Write the simplest failing test** - Start with the most basic scenario from issue
4. **Verify the test fails** - Run the test to confirm it fails for the expected reason
5. **Link test to issue** - Reference issue number in test names and comments

## Red Phase Checklist
- [ ] GitHub issue context retrieved and analysed
- [ ] Test clearly describes expected behaviour from issue requirements
- [ ] Test fails for the right reason (missing implementation)
- [ ] Test name references issue number and describes behaviour
- [ ] Test follows AAA pattern
- [ ] Edge cases from issue discussion considered
- [ ] No production code written yet