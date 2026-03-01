# Safe npm Global Tool Installation

## Overview

The Aspire CLI installs the `@playwright/cli` npm package as a global tool during `aspire agent init`. Because this tool runs with the user's full privileges, we must verify its authenticity and provenance before installation. This document describes the verification process, the threat model, and the reasoning behind each step.

## Threat Model

### What we're protecting against

1. **Registry compromise** — An attacker gains write access to the npm registry and publishes a malicious version of `@playwright/cli`
2. **Publish token theft** — An attacker steals a maintainer's npm publish token and publishes a tampered package
3. **Man-in-the-middle** — An attacker intercepts the network request and substitutes a different tarball
4. **Dependency confusion** — A malicious package with a similar name is installed instead of the intended one

### What we're NOT protecting against

- Compromise of the legitimate source repository (`microsoft/playwright-cli`) itself
- Compromise of the GitHub Actions build infrastructure (Sigstore OIDC provider)
- Compromise of the Sigstore transparency log infrastructure
- Malicious code introduced through legitimate dependencies of `@playwright/cli`

### Trust anchors

Our verification chain relies on these trust anchors:

| Trust anchor | What it provides | How it's protected |
|---|---|---|
| **npm registry** | Package metadata, tarball hosting | HTTPS/TLS, npm's infrastructure security |
| **Sigstore (Fulcio + Rekor)** | Cryptographic attestation signatures | Public CA with OIDC federation, append-only transparency log |
| **GitHub Actions OIDC** | Builder identity claims in Sigstore certificates | GitHub's infrastructure security |
| **Hardcoded expected values** | Package name, version range, expected source repository | Code review, our own release process |

## Verification Process

### Step 1: Resolve package version and metadata

**Action:** Run `npm view @playwright/cli@{versionRange} version` and `npm view @playwright/cli@{version} dist.integrity` to get the resolved version and the registry's SRI integrity hash. The default version range is `>=0.1.1`, which resolves to the latest published version at or above 0.1.1. This can be overridden to a specific version via the `playwrightCliVersion` configuration key.

**What this establishes:** We know the exact version we intend to install and the hash the registry claims for its tarball.

**Trust basis:** npm registry over HTTPS/TLS.

**Limitations:** If the registry is compromised, both the version and hash could be attacker-controlled. This step alone is insufficient — it only establishes what the registry *claims*.

### Step 2: Check if already installed at a suitable version

**Action:** Run `playwright-cli --version` and compare against the resolved version.

**What this establishes:** Whether installation can be skipped entirely (already up-to-date or newer).

**Trust basis:** The previously-installed binary. If the user's system is compromised, this could be spoofed, but that's outside our threat model.

### Step 3: Verify Sigstore attestations via npm

**Action:**
1. Create a temporary directory with a minimal `package.json`
2. Run `npm install @playwright/cli@{version} --ignore-scripts` to install the package from the registry as a project dependency
3. Run `npm audit signatures` to verify Sigstore attestation signatures

**What this establishes:** That valid Sigstore-signed attestations exist for `@playwright/cli@{version}`. Specifically:

- The npm registry has attestation bundles for this package version
- The attestation signatures are cryptographically valid (signed by Sigstore's Fulcio CA)
- The attestation entries are present in the Rekor transparency log (inclusion proof verified)
- The OIDC identity in the signing certificate corresponds to a GitHub Actions workflow

**Trust basis:** Sigstore's public key infrastructure. Even if the npm registry is compromised, an attacker cannot forge valid Sigstore signatures — they would need to compromise Fulcio (the Sigstore CA) or obtain a valid OIDC token from GitHub Actions for the legitimate repository's workflow.

**Why a temporary project is needed:** `npm audit signatures` operates on installed project dependencies. It requires `node_modules` and a `package-lock.json` to know which packages to verify. For a global tool install there is no project context, so we create one temporarily. The package must be installed from the registry (not from a local tarball) because `npm audit signatures` skips packages with `resolved: file:...` in the lockfile.

**Limitations:** `npm audit signatures` verifies that *valid attestations exist* but does not expose the attestation *content*. It confirms "this package has authentic Sigstore-signed attestations" but does not tell us *what* those attestations say (e.g., which repository built the package). That's addressed in Step 4.

### Step 4: Verify provenance metadata

**Action:**
1. Fetch the attestation bundle from `https://registry.npmjs.org/-/npm/v1/attestations/@playwright/cli@{version}`
2. Find the attestation with `predicateType: "https://slsa.dev/provenance/v1"` (SLSA Build L3 provenance)
3. Base64-decode the DSSE envelope payload to extract the in-toto statement
4. Verify the following fields from the provenance predicate:

| Field | Location in payload | Expected value | What it proves |
|---|---|---|---|
| **Source repository** | `predicate.buildDefinition.externalParameters.workflow.repository` | `https://github.com/microsoft/playwright-cli` | The package was built from the legitimate source code |
| **Workflow path** | `predicate.buildDefinition.externalParameters.workflow.path` | `.github/workflows/publish.yml` | The build used the expected CI pipeline, not an ad-hoc or attacker-injected workflow |
| **Build type** | `predicate.buildDefinition.buildType` | `https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1` | The build ran on GitHub Actions, which implicitly confirms the OIDC token issuer is `https://token.actions.githubusercontent.com` |
| **Workflow ref** | `predicate.buildDefinition.externalParameters.workflow.ref` | Validated via caller-provided callback (for `@playwright/cli`: kind=`tags`, name=`v{version}`) | The build was triggered from a version tag matching the package version, not an arbitrary branch or commit. The tag format is package-specific — different packages may use different conventions (e.g., `v0.1.1`, `0.1.1`, `@scope/pkg@0.1.1`). The ref is parsed into structured components (`WorkflowRefInfo`) and the caller provides a validation callback. |

**What this establishes:** That the Sigstore-attested provenance for this package version claims it was built from the `microsoft/playwright-cli` GitHub repository, using the expected publish workflow, on the GitHub Actions CI system, triggered by a git tag that matches the package version. The build type verification implicitly confirms the OIDC token issuer without needing to parse the Sigstore certificate directly — the SLSA build type `https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1` is only valid for GitHub Actions builds that obtain their signing credentials via GitHub's OIDC provider (`https://token.actions.githubusercontent.com`). The workflow ref verification ensures the build corresponds to an explicit version release, preventing an attacker from publishing a package built from an arbitrary branch or commit.

**Additional fields extracted but not directly verified:** The provenance parser also extracts `runDetails.builder.id` from the attestation. This is available in the `NpmProvenanceData` result for logging and diagnostics but is not currently used as a verification gate.

**Trust basis:** The attestation content is protected by the Sigstore signature verified in Step 3. Since Step 3 confirmed the attestation is cryptographically authentic (signed by a valid Sigstore certificate corresponding to a GitHub Actions OIDC identity), the content we read in Step 4 cannot have been tampered with by the npm registry. An attacker would need to compromise Sigstore itself to forge attestation content pointing to `microsoft/playwright-cli` with the correct workflow and build type.

**Why we verify all three fields:** Checking only the source repository would leave a gap where an attacker with write access to the repo could introduce a malicious workflow (e.g., `.github/workflows/evil.yml`) that builds a tampered package. By also verifying the workflow path and build type, we ensure the package was built by the specific, expected CI pipeline running on the expected CI system.

**Why we fetch from the registry API:** The npm CLI (`npm audit signatures`) verifies Sigstore signatures but does not expose provenance content in its output. The `--json` flag only produces `{"invalid":[],"missing":[]}`. There is no npm CLI command to read the source repository from a SLSA provenance attestation. We must fetch the attestation bundle directly from the registry API.

**Note on reading attested content from an untrusted source:** We are reading the attestation JSON from the same npm registry that could theoretically be compromised. However, the attestation *signature* was already verified by `npm audit signatures` in Step 3 using Sigstore's independent trust chain. We are relying on the fact that the registry serves the same attestation bundle that npm verified. If the registry served different attestation data to our HTTP request than what `npm audit signatures` verified, the provenance content could be spoofed. This is a residual risk — see "Residual Risks" below.

### Step 5: Download and verify tarball integrity

**Action:**
1. Run `npm pack @playwright/cli@{version}` to download the tarball
2. Compute SHA-512 hash of the downloaded tarball
3. Compare against the SRI integrity hash obtained in Step 1

**What this establishes:** That the tarball we have on disk is bit-for-bit identical to what the npm registry published for this version.

**Trust basis:** Cryptographic hash comparison (SHA-512). If the hash matches, the content is the same regardless of how it was delivered.

**Relationship to Step 3:** The Sigstore attestations verified in Step 3 are bound to the package version and its published content. The integrity hash in the registry packument is the canonical identifier for the tarball content. By verifying our tarball matches this hash, we establish that our tarball is the same artifact that the Sigstore attestations cover.

### Step 6: Install globally from verified tarball

**Action:** Run `npm install -g {tarballPath}` to install the verified tarball as a global tool.

**What this establishes:** The tool is installed and available on the user's PATH.

**Trust basis:** All preceding verification steps have passed. The tarball content has been verified against the registry's published hash (Step 5), the Sigstore attestations for that content are cryptographically valid (Step 3), and the attestations claim the correct source repository (Step 4).

### Step 7: Generate and mirror skill files

**Action:** Run `playwright-cli install --skills` to generate agent skill files in the primary skill directory (`.claude/skills/playwright-cli/`), then mirror the skill directory to all other detected agent environment skill directories (e.g., `.github/skills/playwright-cli/`, `.opencode/skill/playwright-cli/`). The mirror is a full sync — files are created, updated, and stale files are removed so all environments have identical skill content.

**What this establishes:** The Playwright CLI skill files are available for all configured agent environments.

## Verification Chain Summary

```text
                    ┌──────────────────────────────┐
                    │   Hardcoded expectations      │
                    │   • Package: @playwright/cli  │
                    │   • Version range: >=0.1.1    │
                    │   • Source: microsoft/         │
                    │     playwright-cli             │
                    │   • Workflow: .github/         │
                    │     workflows/publish.yml      │
                    │   • Build type: GitHub Actions │
                    │     workflow/v1                │
                    └──────────────┬────────────────┘
                                   │
                    ┌──────────────▼────────────────┐
                    │  Step 1: Resolve version +     │
                    │  integrity hash from registry  │
                    └──────────────┬────────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                     │
   ┌──────────▼──────────┐  ┌─────▼──────────┐  ┌──────▼──────────┐
   │ Step 3: npm audit    │  │ Step 4: Verify  │  │ Step 5: npm pack│
   │ signatures           │  │ provenance      │  │ + SHA-512 check │
   │ (Sigstore crypto)    │  │ (repo, workflow │  │ (tarball        │
   │                      │  │  + build type)  │  │  integrity)     │
   └──────────┬───────────┘  └─────┬──────────┘  └──────┬──────────┘
              │                    │                     │
              │  Attestation is    │  Built from         │  Tarball matches
              │  authentic         │  expected repo +    │  published hash
              │                    │  expected pipeline  │
              └────────────────────┼─────────────────────┘
                                   │
                    ┌──────────────▼────────────────┐
                    │  Step 6: npm install -g        │
                    │  (from verified tarball)        │
                    └───────────────────────────────┘
```

## Residual Risks

### 1. Registry serving different attestation data to different clients

**Risk:** The npm registry could theoretically serve one attestation bundle to `npm audit signatures` (which passes verification) and a different bundle to our HTTP API request (with spoofed provenance content).

**Mitigation:** This would require active, targeted compromise of the npm registry's serving infrastructure — not just a publish token theft or package tampering. The Rekor transparency log provides a public record of all attestations, making such targeted serving detectable.

**Alternative mitigation:** We could eliminate this risk entirely by parsing the attestation bundle ourselves and verifying the Sigstore signature directly in C#. This would require implementing ECDSA signature verification, X.509 certificate chain validation, and Merkle inclusion proof verification. This significantly increases implementation complexity and is not recommended for the initial implementation.

### 2. Time-of-check-to-time-of-use (TOCTOU)

**Risk:** The package could be replaced on the registry between our verification steps and the global install.

**Mitigation:** We verify the SHA-512 hash of the tarball we actually install (Step 5), and we install from the local tarball file (not from the registry again). The verified tarball is the same file that gets installed.

### 3. Transitive dependency attacks

**Risk:** `@playwright/cli` has dependencies that could be compromised.

**Mitigation:** The `--ignore-scripts` flag prevents execution of install scripts. However, the dependencies' code runs when the tool is invoked. This is partially mitigated by Sigstore attestations covering the dependency tree, but comprehensive supply chain verification of all transitive dependencies is out of scope.

## Implementation Constants

```csharp
internal const string PackageName = "@playwright/cli";
internal const string VersionRange = ">=0.1.1";
internal const string ExpectedSourceRepository = "https://github.com/microsoft/playwright-cli";
internal const string ExpectedWorkflowPath = ".github/workflows/publish.yml";
internal const string ExpectedBuildType = "https://slsa-framework.github.io/github-actions-buildtypes/workflow/v1";
internal const string NpmRegistryAttestationsBaseUrl = "https://registry.npmjs.org/-/npm/v1/attestations";
internal const string SlsaProvenancePredicateType = "https://slsa.dev/provenance/v1";
```

## Configuration

Two break-glass configuration keys are available via `aspire config set`:

| Key | Effect |
|---|---|
| `disablePlaywrightCliPackageValidation` | When `"true"`, skips all Sigstore, provenance, and integrity checks. Use only for debugging npm service issues. |
| `playwrightCliVersion` | When set, overrides the version range and pins to the specified exact version. |

## Future Improvements

1. **Direct Sigstore verification in .NET** — An experimental implementation exists on the `sigstore-builtin-verification` branch using the [`Sigstore`](https://github.com/mitchdenny/sigstore-dotnet) .NET library. This eliminates residual risk #1 by performing Sigstore bundle verification natively (Fulcio certificate chain, Rekor transparency log inclusion proof, SCT verification, DSSE signature verification) without relying on `npm audit signatures`. It is gated behind the `builtInSigstoreVerificationEnabled` feature flag.
2. **Certificate identity verification** — The experimental Sigstore path already verifies the OIDC identity claims in the Sigstore signing certificate (issuer and SAN) via `CertificateIdentity.ForGitHubActions(repository)`. Bringing this to the default path would provide defense-in-depth beyond the provenance payload checks.
3. **Pinned tarball hash** — Ship a known-good SRI hash with each Aspire release, eliminating the need to trust the registry for the hash at all.
