---
name: Release Checklist
about: Track manual tasks for a .NET Aspire release
title: 'Release Checklist: .NET Aspire VERSION'
labels: area-infrastructure, release
assignees: ''
---

# Release Checklist for .NET Aspire VERSION

This issue tracks the manual tasks required for the VERSION release.

## Automated Tasks ✅

The following tasks are handled by automation workflows:

- [ ] Create version tag (vVERSION)
- [ ] Build and publish packages
- [ ] Create GitHub Release with CLI archives
- [ ] Update aspire-samples repository
- [ ] Update dotnet-docker repository
- [ ] Bump PackageValidationBaselineVersion

## Manual Tasks

### Pre-Release

- [ ] Verify all release blockers are resolved
- [ ] Coordinate with area owners on feature completeness
- [ ] Review and approve any pending security fixes
- [ ] Run release coordinator workflow: [release-coordinator.yml](../../actions/workflows/release-coordinator.yml)

### Release Process

- [ ] Verify NuGet packages are published: https://www.nuget.org/packages?q=Aspire
- [ ] Verify GitHub release is created: https://github.com/dotnet/aspire/releases/tag/vVERSION
- [ ] Test CLI installation: `dotnet tool install -g Aspire.Cli --version VERSION`
- [ ] Verify VS Code extension is published to marketplace (if applicable)
- [ ] Test dashboard functionality with new version

### Post-Release

- [ ] Review and merge [aspire-samples](https://github.com/dotnet/aspire-samples/pulls) update PR
- [ ] Review and merge [dotnet-docker](https://github.com/dotnet/dotnet-docker/pulls) update PR
- [ ] Review and merge baseline version bump PR
- [ ] Update aka.ms/aspire-cli links (if needed)
- [ ] Communicate with validation team
- [ ] Write and publish blog post announcement
- [ ] Update documentation on [Microsoft Learn](https://learn.microsoft.com/dotnet/aspire/)
- [ ] Update [release notes](https://github.com/dotnet/aspire/releases)

### Compliance & Communication

- [ ] Complete compliance sign-offs
- [ ] Send release announcement to internal mailing lists
- [ ] Post on social media channels (Twitter, LinkedIn, etc.)
- [ ] Update community Discord/Slack channels
- [ ] Send announcement to .NET Foundation newsletter

## Reference Links

- **Release Wiki:** https://github.com/dotnet/aspire/wiki/New-Release-tictoc
- **Automation Plan:** [docs/infra/automation-plan.md](../../../docs/infra/automation-plan.md)
- **NuGet Packages:** https://www.nuget.org/packages?q=Aspire
- **Release Workflow:** [release.yml](../../workflows/release.yml)
- **Release Coordinator:** [release-coordinator.yml](../../workflows/release-coordinator.yml)

---

**Version:** VERSION  
**Target Date:** YYYY-MM-DD  
**Release Manager:** @mention-release-manager
