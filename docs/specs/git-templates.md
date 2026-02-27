# Git-Based Template System for Aspire

**Status:** Draft
**Authors:** Aspire CLI Team
**Last Updated:** 2026-02-27

## 1. Overview

This spec defines a git-based templating system for the Aspire CLI's `aspire new` command. This is a new capability layered on top of the existing template infrastructure — it does not replace `dotnet new`. The `dotnet new` mechanism continues to serve the broader .NET developer ecosystem and reflects Aspire's heritage before the Aspire CLI existed. Both systems coexist: `dotnet new` for developers who prefer the standard .NET workflow, and `aspire new` for a richer, polyglot-friendly, community-oriented experience with git-based template discovery.

### Motivation: Templates Should Be Effortless

The best template ecosystems share a common trait: the distance between "I built something useful" and "anyone can use this as a starting point" is nearly zero. Today, creating an Aspire template requires packaging it as a NuGet package with `.template.config/template.json`, understanding the dotnet templating engine's symbol system, and publishing to a feed. This friction means that most useful Aspire applications never become templates, even when their authors would happily share them.

By making templates git repositories, we eliminate this friction entirely. An Aspire developer's natural workflow — build an app, push it to GitHub — becomes the template authoring workflow. Adding a single `aspire-template.json` file to the repo root is all it takes to make the project available as a template to anyone in the world.

This has a compounding community effect:

- **Every public Aspire app is a potential template.** Developers who build interesting Aspire applications can share them with a single file addition. There's no separate "template authoring" skill to learn.
- **Organizations can standardize organically.** Teams push their company-standard Aspire setup to `github.com/{org}/aspire-templates` and every developer in the org automatically sees it in `aspire new`.
- **The ecosystem self-curates.** Popular templates get GitHub stars, forks, and community contributions. The best templates rise naturally through the same mechanisms that make open source work.
- **Polyglot templates are first-class citizens.** A TypeScript Aspire app and a C# Aspire app are both just directories of files. The same template system works for both without any language-specific plumbing.

### Design Principles

1. **Templates are real apps.** A template is a working Aspire AppHost project. Template authors develop, run, and test their templates as normal Aspire applications. The template engine personalizes the app via string substitution.
2. **Git-native distribution.** Templates are hosted in git repositories (GitHub initially). No NuGet packaging, no custom registries. If you can push to git, you can publish a template.
3. **Discoverable by default.** The CLI automatically discovers templates from official, personal, and organizational sources. Users don't need to configure anything to find useful templates.
4. **Polyglot from day one.** Templates work for any language Aspire supports — C#, TypeScript, Python, Go, Java, Rust — because they're just real projects with variable substitution.
5. **Secure by design.** Templates are static file trees. No arbitrary code execution during template application. What you see in the repo is what you get.
6. **Zero-friction authoring.** Adding a single `aspire-template.json` file to any Aspire app repo makes it a template. No packaging, no special tooling, no separate publishing step.

### Goals

- Enable community-contributed templates without requiring access to the Aspire repo
- Support templates in any Aspire-supported language
- Provide federated template discovery across official, personal, and org sources
- Make template authoring as simple as "build an Aspire app, add a manifest"
- Maintain security guarantees — no supply chain risk from template application

### Non-Goals

- Deprecating or replacing `dotnet new` — that infrastructure serves the .NET ecosystem and will continue to exist alongside this system
- Building a template marketplace with ratings, reviews, or social features (out of scope for v1)
- Supporting non-git template hosting (e.g., OCI registries, NuGet packages) in the initial release
- Adding git-based template discovery to `dotnet new` — this is an Aspire CLI (`aspire new`) capability

## 2. Concepts

### Template

A **template** is a directory within a git repository that contains:

1. A working Aspire application (AppHost + service projects)
2. An `aspire-template.json` manifest describing the template's metadata, variables, and substitution rules

Because templates are real Aspire applications, template authors develop them using the normal Aspire workflow: `dotnet run`, `aspire run`, etc. The template engine's only job is to copy the files and apply string replacements to personalize the output.

### Template Index

A **template index** (`aspire-template-index.json`) is a JSON file at the root of a git repository that catalogs available templates. An index can:

- List templates contained within the same repository
- Reference templates in external repositories
- Link to other template indexes (federation)

The CLI walks the graph of indexes to build a unified template catalog.

### Single-Template Repositories

A repository doesn't need an index file if it contains a single template. If a repo has an `aspire-template.json` at its root but no `aspire-template-index.json`, the CLI treats it as an implicit index of one — the template IS the repo.

This is the most common case for community templates. A developer builds an Aspire app, adds `aspire-template.json` to the root, and their repo is immediately usable with `aspire new --template-repo`:

```
my-cool-aspire-app/
├── aspire-template.json        # Makes this repo a template
├── MyCoolApp.sln
├── MyCoolApp.AppHost/
│   ├── Program.cs
│   └── MyCoolApp.AppHost.csproj
└── MyCoolApp.Web/
    └── ...
```

If both `aspire-template-index.json` AND `aspire-template.json` exist at the root, the index file takes precedence and the root-level `aspire-template.json` is used as one of the indexed templates (with an implicit `"path": "."`).

### Template Source

A **template source** is a git repository that the CLI checks for templates. Sources are resolved in priority order:

| Priority | Source | Repository Pattern | Condition |
|----------|--------|-------------------|-----------|
| 1 | Official | `github.com/dotnet/aspire` (default branch: `release/latest`) | Always checked |
| 2 | Personal | `github.com/{username}/aspire-templates` | GitHub CLI authenticated |
| 3 | Org | `github.com/{org}/aspire-templates` | GitHub CLI authenticated, user consent |
| 4 | Explicit | Any URL or local path | `--template-repo` flag |

## 3. Schema: `aspire-template-index.json`

The template index file lives at the root of a repository and describes the templates available from that source.

```json
{
  "$schema": "https://aka.ms/aspire/template-index-schema/v1",
  "version": 1,
  "publisher": {
    "name": "Microsoft",
    "url": "https://github.com/microsoft",
    "verified": true
  },
  "templates": [
    {
      "name": "aspire-starter",
      "displayName": "Aspire Starter Application",
      "description": "A full-featured Aspire starter with a web frontend and API backend.",
      "path": "templates/aspire-starter",
      "language": "csharp",
      "tags": ["starter", "web", "api"],
      "minAspireVersion": "9.0.0"
    },
    {
      "name": "aspire-ts-starter",
      "displayName": "Aspire TypeScript Starter",
      "description": "An Aspire application with a TypeScript AppHost.",
      "path": "templates/aspire-ts-starter",
      "language": "typescript",
      "tags": ["starter", "typescript", "polyglot"],
      "minAspireVersion": "9.2.0"
    },
    {
      "name": "contoso-microservices",
      "displayName": "Contoso Microservices Template",
      "description": "A production-grade microservices template maintained by the Contoso team.",
      "repo": "https://github.com/contoso/aspire-microservices-template",
      "path": ".",
      "language": "csharp",
      "tags": ["microservices", "production", "partner"]
    }
  ],
  "includes": [
    {
      "url": "https://github.com/azure/aspire-azure-templates",
      "description": "Azure-specific Aspire templates"
    },
    {
      "url": "https://github.com/aws/aspire-aws-templates",
      "description": "AWS-specific Aspire templates"
    }
  ]
}
```

### Field Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `$schema` | string | No | JSON schema URL for validation and editor support |
| `version` | integer | Yes | Schema version. Must be `1` for this spec. |
| `publisher` | object | No | Information about the publisher of this index |
| `publisher.name` | string | Yes (if publisher) | Display name of the publisher |
| `publisher.url` | string | No | URL of the publisher |
| `publisher.verified` | boolean | No | Whether this is a verified publisher (set by official Aspire infrastructure) |
| `templates` | array | Yes | List of template entries |
| `templates[].name` | string | Yes | Unique machine-readable template identifier (kebab-case) |
| `templates[].displayName` | string | Yes | Human-readable template name |
| `templates[].description` | string | Yes | Short description of the template |
| `templates[].path` | string | Yes | Path to the template directory, relative to the repo root (or the external repo root if `repo` is specified) |
| `templates[].repo` | string | No | Git URL of an external repository containing the template. If omitted, the template is in the same repo as the index. |
| `templates[].language` | string | No | Primary language of the template (`csharp`, `typescript`, `python`, `go`, `java`, `rust`). If omitted, the template is language-agnostic. |
| `templates[].tags` | array | No | Tags for filtering and categorization |
| `templates[].minAspireVersion` | string | No | Minimum Aspire version required |
| `includes` | array | No | Other template indexes to include (federation) |
| `includes[].url` | string | Yes | Git URL of the repository containing the included index |
| `includes[].description` | string | No | Description of the included index source |

## 4. Schema: `aspire-template.json`

The template manifest lives inside a template directory and describes how to apply the template.

```json
{
  "$schema": "https://aka.ms/aspire/template-schema/v1",
  "version": 1,
  "name": "aspire-starter",
  "displayName": "Aspire Starter Application",
  "description": "A full-featured Aspire starter with a web frontend and API backend.",
  "language": "csharp",
  "variables": {
    "projectName": {
      "displayName": "Project Name",
      "description": "The name for your new Aspire application.",
      "type": "string",
      "required": true,
      "defaultValue": "AspireApp",
      "validation": {
        "pattern": "^[A-Za-z][A-Za-z0-9_.]*$",
        "message": "Project name must start with a letter and contain only letters, digits, dots, and underscores."
      }
    },
    "useRedisCache": {
      "displayName": "Include Redis Cache",
      "description": "Add a Redis cache resource to the AppHost.",
      "type": "boolean",
      "required": false,
      "defaultValue": false
    },
    "testFramework": {
      "displayName": "Test Framework",
      "description": "The test framework to use for the test project.",
      "type": "choice",
      "required": false,
      "choices": [
        { "value": "xunit", "displayName": "xUnit.net", "description": "xUnit.net test framework" },
        { "value": "nunit", "displayName": "NUnit", "description": "NUnit test framework" },
        { "value": "mstest", "displayName": "MSTest", "description": "MSTest test framework" }
      ],
      "defaultValue": "xunit"
    },
    "httpPort": {
      "displayName": "HTTP Port",
      "description": "The HTTP port for the web frontend.",
      "type": "integer",
      "required": false,
      "defaultValue": 5000,
      "validation": {
        "min": 1024,
        "max": 65535
      }
    }
  },
  "substitutions": {
    "filenames": {
      "AspireStarter": "{{projectName}}"
    },
    "content": {
      "AspireStarter": "{{projectName}}",
      "aspirestarter": "{{projectName | lowercase}}",
      "ASPIRE_STARTER": "{{projectName | uppercase}}"
    }
  },
  "conditionalFiles": {
    "tests/": "{{testFramework}}",
    "AspireStarter.AppHost/redis-config.json": "{{useRedisCache}}"
  },
  "postMessages": [
    "Your Aspire application '{{projectName}}' has been created!",
    "Run `cd {{projectName}} && dotnet run --project {{projectName}}.AppHost` to start the application."
  ]
}
```

### Field Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `$schema` | string | No | JSON schema URL for validation and editor support |
| `version` | integer | Yes | Schema version. Must be `1` for this spec. |
| `name` | string | Yes | Machine-readable template identifier (must match index entry) |
| `displayName` | string | Yes | Human-readable template name |
| `description` | string | Yes | Short description |
| `language` | string | No | Primary language |
| `variables` | object | Yes | Map of variable name → variable definition |
| `substitutions` | object | Yes | Substitution rules |
| `substitutions.filenames` | object | No | Map of filename patterns → replacement expressions |
| `substitutions.content` | object | No | Map of content patterns → replacement expressions |
| `conditionalFiles` | object | No | Files/directories conditionally included based on variable values |
| `postMessages` | array | No | Messages displayed to the user after template application |

### Variable Types

| Type | Description | Additional Properties |
|------|-------------|----------------------|
| `string` | Free-text string | `validation.pattern`, `validation.message` |
| `boolean` | True/false | — |
| `choice` | Selection from predefined options | `choices` array |
| `integer` | Numeric integer | `validation.min`, `validation.max` |

### Substitution Expressions

Substitution values use a lightweight expression syntax:

| Expression | Description | Example Input | Example Output |
|------------|-------------|---------------|----------------|
| `{{variableName}}` | Direct substitution | `MyApp` | `MyApp` |
| `{{variableName \| lowercase}}` | Lowercase | `MyApp` | `myapp` |
| `{{variableName \| uppercase}}` | Uppercase | `MyApp` | `MYAPP` |
| `{{variableName \| kebabcase}}` | Kebab-case | `MyApp` | `my-app` |
| `{{variableName \| snakecase}}` | Snake_case | `MyApp` | `my_app` |
| `{{variableName \| camelcase}}` | camelCase | `MyApp` | `myApp` |
| `{{variableName \| pascalcase}}` | PascalCase | `myApp` | `MyApp` |

### Conditional Files

The `conditionalFiles` section controls which files are included in the output:

- **Boolean variables:** File is included only when the variable is `true`.
- **Choice variables:** File/directory is included only when the variable has a truthy (non-empty) value. For more granular control, use the naming convention `{{variableName}}-xunit/` where the directory name encodes the choice.

## 5. Template Directory Structure

A template repository using this system looks like:

```
aspire-templates/
├── aspire-template-index.json          # Root index file
├── templates/
│   ├── aspire-starter/
│   │   ├── aspire-template.json        # Template manifest
│   │   ├── AspireStarter.sln           # Working solution (template author can dotnet run this)
│   │   ├── AspireStarter.AppHost/
│   │   │   ├── Program.cs
│   │   │   └── AspireStarter.AppHost.csproj
│   │   ├── AspireStarter.Web/
│   │   │   ├── Program.cs
│   │   │   └── AspireStarter.Web.csproj
│   │   └── AspireStarter.ApiService/
│   │       ├── Program.cs
│   │       └── AspireStarter.ApiService.csproj
│   │
│   ├── aspire-ts-starter/
│   │   ├── aspire-template.json
│   │   ├── apphost.ts
│   │   ├── package.json
│   │   └── services/
│   │       └── api/
│   │           ├── index.ts
│   │           └── package.json
│   │
│   └── aspire-empty/
│       ├── aspire-template.json
│       ├── AspireEmpty.sln
│       └── AspireEmpty.AppHost/
│           ├── Program.cs
│           └── AspireEmpty.AppHost.csproj
```

### Key Insight: The Template IS the App

The `AspireStarter` directory is a fully functional Aspire application. The template author can:

```bash
cd templates/aspire-starter
dotnet run --project AspireStarter.AppHost
```

This runs the template as a real application. The template engine's job is simply to:
1. Copy the directory
2. Replace `AspireStarter` → `MyProjectName` in filenames and content
3. Exclude/include conditional files
4. Display post-creation messages

## 6. Template Resolution

When a user runs `aspire new`, the CLI resolves available templates through a multi-phase process.

### Phase 1: Index Collection

```
                    ┌──────────────────────┐
                    │  Official Index      │
                    │  microsoft/          │
                    │  aspire-templates    │
                    └──────┬───────────────┘
                           │
              ┌────────────┼────────────────┐
              ▼            ▼                ▼
     ┌────────────┐  ┌──────────┐   ┌──────────────┐
     │ Built-in   │  │ Included │   │ Included     │
     │ Templates  │  │ Index:   │   │ Index:       │
     │            │  │ azure/   │   │ aws/         │
     │            │  │ aspire-  │   │ aspire-      │
     │            │  │ templates│   │ templates    │
     └────────────┘  └──────────┘   └──────────────┘

     ┌──────────────────────┐  ┌──────────────────────┐
     │  Personal Index      │  │  Org Indexes         │
     │  {user}/             │  │  {org}/              │
     │  aspire-templates    │  │  aspire-templates    │
     └──────────────────────┘  └──────────────────────┘
```

**Resolution order:**
1. Check local cache. If cache is valid (within TTL), use cached index.
2. Fetch `aspire-template-index.json` from official repo via shallow sparse clone.
3. Walk `includes` references, fetching each linked index (cycle detection + depth limit of 5).
4. If GitHub CLI is authenticated, check personal and org repos.
5. Merge all templates into a unified catalog, preserving source metadata.

**Index resolution per repository:**
When the CLI checks a repository for templates, it follows this logic:

1. Look for `aspire-template-index.json` at the repo root → use as the index.
2. If no index found, look for `aspire-template.json` at the repo root → treat as a single-template index (the template path is `.`, metadata comes from the manifest).
3. If both exist, use the index file. If the index doesn't already reference a template at `"path": "."`, the root `aspire-template.json` is implicitly included as an additional template.
4. If neither file exists, the repository is not a template source (skip silently for auto-discovered sources, warn for explicit `--template-repo`).

### Phase 2: Template Selection

The user selects a template through one of:

- **Direct name:** `aspire new aspire-starter`
- **Explicit repo:** `aspire new --template-repo https://github.com/contoso/templates --template-name my-template`
- **Interactive:** `aspire new` → prompted with categorized template list
- **Language filter:** `aspire new --language typescript` → only TypeScript templates shown

### Phase 3: Template Fetch

Once a template is selected:

1. If the template is in a remote repo, perform a shallow sparse clone targeting only the template's `path`.
2. Read the `aspire-template.json` manifest.
3. Prompt the user for any required variables (with defaults pre-filled).
4. Apply the template.

## 7. Template Application

Template application is a deterministic, side-effect-free process:

```
Input:  Template directory + variable values
Output: New project directory
```

### Algorithm

```
1. COPY template directory → output directory
   - Skip: aspire-template.json (manifest is not part of output)
   - Skip: .git/, .github/ (git metadata from template repo)
   - Skip: files excluded by conditionalFiles rules

2. RENAME files and directories
   - For each entry in substitutions.filenames:
     Replace pattern with evaluated expression in all file/directory names
   - Process deepest paths first (to avoid renaming parent before child)

3. SUBSTITUTE content
   - For each file in output directory:
     For each entry in substitutions.content:
       Replace all occurrences of pattern with evaluated expression
   - Binary files are detected and skipped (by extension or content sniffing)

4. DISPLAY post-creation messages
   - Evaluate variable expressions in postMessages
   - Print to console
```

### Binary File Handling

The template engine skips content substitution for binary files. Binary detection uses:

1. **Extension allowlist:** `.png`, `.jpg`, `.gif`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.pdf`, `.zip`, `.dll`, `.exe`, `.so`, `.dylib`
2. **Content sniffing fallback:** Check first 8KB for null bytes

### .gitignore Respect

Files matched by a `.gitignore` in the template directory are excluded from the output. This allows template authors to have local build artifacts that don't get copied.

## 8. Caching Strategy

### Index Cache

- **Location:** `~/.aspire/template-cache/indexes/`
- **TTL:** 1 hour for official index, 4 hours for community indexes
- **Background refresh:** When the CLI starts, if the cache is stale, fetch updated indexes in the background. The current operation uses the cached data.
- **Force refresh:** `aspire new --refresh` forces a fresh fetch.

### Template Cache

- **Location:** `~/.aspire/template-cache/templates/{source-hash}/{template-name}/`
- **Strategy:** Templates are cached after first fetch. Cache is keyed by repo URL + commit SHA.
- **Invalidation:** When the index is refreshed and a template's source has changed, the cached template is invalidated.

### Cache Layout

```
~/.aspire/
└── template-cache/
    ├── indexes/
    │   ├── microsoft-aspire-templates.json     # Cached official index
    │   ├── azure-aspire-azure-templates.json   # Cached included index
    │   └── cache-metadata.json                 # TTL tracking
    └── templates/
        ├── a1b2c3d4/                           # Hash of repo URL
        │   ├── aspire-starter/                 # Cloned template content
        │   └── aspire-ts-starter/
        └── e5f6g7h8/
            └── contoso-microservices/
```

## 9. CLI Integration

### Feature Flag

The entire git-based template system is gated behind a feature flag:

```
features.gitTemplatesEnabled = true|false (default: false)
```

When disabled, the `aspire template` command tree is hidden and `GitTemplateFactory` does not register any templates. This allows incremental development without affecting existing users.

### Configuration Values

Template sources are configured as named entries under the `templates.indexes` key. Each entry is an object with `repo` and optional `ref` properties. This uses the CLI's hierarchical config system — dotted keys are stored as nested JSON objects.

**Index configuration:**

| Key Pattern | Type | Description |
|-------------|------|-------------|
| `templates.indexes.<name>.repo` | string | Git URL or local file path for the template index source. |
| `templates.indexes.<name>.ref` | string | Git ref to use (branch, tag, or commit SHA). Optional — defaults to `templates.defaultBranch` if not set. |

The name `default` is special — it refers to the official Aspire template index. If not configured, it defaults to `https://github.com/dotnet/aspire` at the `release/latest` branch.

```bash
# Override the default (official) template index
aspire config set -g templates.indexes.default.repo https://github.com/mitchdenny/aspire-templates-override

# Point default at a specific branch (e.g., testing a PR)
aspire config set -g templates.indexes.default.ref refs/pull/14500/head

# Add a company template index
aspire config set -g templates.indexes.contoso.repo https://github.com/contoso/aspire-templates

# Add a local template index for development/testing
aspire config set -g templates.indexes.local.repo C:\Code\mylocaltemplatetests

# Point at a specific release tag
aspire config set -g templates.indexes.contoso.ref v2.1.0

# Add another team's templates pinned to main
aspire config set -g templates.indexes.platform-team.repo https://github.com/contoso/platform-aspire-templates
aspire config set -g templates.indexes.platform-team.ref main
```

This produces the following in `~/.aspire/globalsettings.json`:

```json
{
  "templates": {
    "indexes": {
      "default": {
        "repo": "https://github.com/mitchdenny/aspire-templates-override",
        "ref": "refs/pull/14500/head"
      },
      "contoso": {
        "repo": "https://github.com/contoso/aspire-templates",
        "ref": "v2.1.0"
      },
      "local": {
        "repo": "C:\\Code\\mylocaltemplatetests"
      },
      "platform-team": {
        "repo": "https://github.com/contoso/platform-aspire-templates",
        "ref": "main"
      }
    }
  }
}
```

The `ref` field is particularly useful for:
- **Testing PR changes:** `aspire config set -g templates.indexes.default.ref refs/pull/14500/head`
- **Pinning to a release:** `aspire config set -g templates.indexes.contoso.ref v2.1.0`
- **Tracking a branch:** `aspire config set -g templates.indexes.default.ref main`

**Other configuration keys:**

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `features.gitTemplatesEnabled` | bool | `false` | Enable/disable the git-based template system |
| `templates.defaultBranch` | string | `release/latest` | Default branch when a specific index entry doesn't specify a `ref`. |
| `templates.cacheTtlMinutes` | int | `60` | Cache TTL for template indexes in minutes |
| `templates.enablePersonalDiscovery` | bool | `true` | Auto-discover `{username}/aspire-templates` when GitHub CLI is authenticated |
| `templates.enableOrgDiscovery` | bool | `true` | Auto-discover `{org}/aspire-templates` for the user's GitHub organizations |

```bash
# Use main branch as the default for all indexes that don't specify a ref
aspire config set -g templates.defaultBranch main

# Disable auto-discovery of personal templates
aspire config set -g templates.enablePersonalDiscovery false
```

**Resolution order for indexes:**

1. All entries under `templates.indexes.*` (including `default` if present; otherwise the built-in `https://github.com/dotnet/aspire` at `templates.defaultBranch` is used)
2. Personal: `github.com/{username}/aspire-templates` (if `enablePersonalDiscovery` is `true` and GitHub CLI is authenticated)
3. Org: `github.com/{org}/aspire-templates` for each org (if `enableOrgDiscovery` is `true` and GitHub CLI is authenticated)
4. Explicit: `--template-repo` flag on individual commands

To remove a configured index, use `aspire config delete -g templates.indexes.<name>.repo`. To completely disable the official index, remove the `default` entry and set `enablePersonalDiscovery` and `enableOrgDiscovery` to `false`.

### Default Template Index Source

The default template index lives in the main Aspire repository itself, not a separate repo. This keeps the official templates close to the codebase and versioned alongside Aspire releases:

- **Default URL:** `https://github.com/dotnet/aspire` (the main Aspire repo)
- **Default branch:** `release/latest` (the latest release branch, ensuring stability)
- **Index path:** `templates/aspire-template-index.json` (a new directory in the Aspire repo)

When the user overrides `templates.defaultBranch` to `main`, they get templates that track the latest development. This is useful for testing template changes before a release.

### Command Tree: `aspire template`

The `aspire template` command group provides template management and authoring tools. All subcommands are behind the `gitTemplatesEnabled` feature flag.

```
aspire template
├── list              List available templates from all sources
├── search <keyword>  Search templates by name, description, or tags
├── refresh           Force refresh the template cache
├── new [path]        Scaffold a new aspire-template.json manifest
└── new-index [path]  Scaffold a new aspire-template-index.json index file
```

#### `aspire template list`

Lists all available templates from all configured sources, grouped by source:

```
$ aspire template list

Official (dotnet/aspire @ release/9.2)
  Name                     Language    Description
  aspire-starter           C#          Full-featured Aspire starter application
  aspire-ts-starter        TypeScript  Aspire with TypeScript AppHost
  aspire-py-starter        Python      Aspire with Python AppHost

Azure (azure/aspire-azure-templates)
  Name                     Language    Description
  aspire-azure-functions   C#          Aspire with Azure Functions

Personal (yourname/aspire-templates)
  Name                     Language    Description
  my-starter               C#          My company starter template

Options:
  --language <lang>        Filter by language (csharp, typescript, python, etc.)
  --source <name>          Filter by source (official, personal, org, or source URL)
  --json                   Output as JSON (for automation/scripting)
```

#### `aspire template search <keyword>`

Searches templates by keyword across names, descriptions, and tags:

```
$ aspire template search redis

Results for "redis":
  Name                     Source              Language    Description
  aspire-redis-starter     official            C#          Aspire starter with Redis cache
  redis-microservices      contoso/templates   C#          Microservices pattern with Redis

Options:
  --language <lang>        Filter by language
  --json                   Output as JSON
```

#### `aspire template refresh`

Forces a refresh of all cached template indexes and invalidates cached template content:

```
$ aspire template refresh

Refreshing template indexes...
  ✓ dotnet/aspire @ release/9.2 (12 templates)
  ✓ azure/aspire-azure-templates (3 templates)
  ✓ yourname/aspire-templates (2 templates)
  ✗ contoso/aspire-templates (not found)

17 templates available from 3 sources.
```

#### `aspire template new [path]`

Scaffolds a new `aspire-template.json` manifest file. This helps template authors get started:

```
$ aspire template new

Creating aspire-template.json...

? Template name (kebab-case): my-cool-template
? Display name: My Cool Template
? Description: A template for building cool things with Aspire
? Primary language: csharp
? Canonical project name to replace: MyCoolTemplate

Created aspire-template.json with substitution rules for "MyCoolTemplate".
Next steps:
  1. Review and customize aspire-template.json
  2. Test with: aspire new --template-repo . --name TestApp
  3. Push to git and share!
```

If `[path]` is provided, creates the manifest at that path instead of the current directory.

#### `aspire template new-index [path]`

Scaffolds a new `aspire-template-index.json` index file for multi-template repositories or organizational indexes:

```
$ aspire template new-index

Creating aspire-template-index.json...

? Publisher name: Contoso
? Publisher URL (optional): https://github.com/contoso

Created aspire-template-index.json.
Add templates to the "templates" array to make them discoverable.
```

### Modified Commands

#### `aspire new`

When `gitTemplatesEnabled` is `true`, `aspire new` shows templates from both the existing `DotNetTemplateFactory` and the new `GitTemplateFactory`. Git-sourced templates appear alongside built-in templates:

```
aspire new [template-name] [options]

Arguments:
  template-name    Name of the template to use (optional, interactive if omitted)

Options:
  -n, --name <name>              Project name
  -o, --output <path>            Output directory
  --language <lang>              Filter templates by language
  --template-repo <url|path>     Use a git template from a specific repo or local path
  --template-name <name>         Template name within the specified repo
```

When `--template-repo` is used, the CLI fetches the specified repo, reads its `aspire-template-index.json` or `aspire-template.json`, and applies the template directly — bypassing normal discovery.

## 10. Security Model

### Threat Model

| Threat | Mitigation |
|--------|-----------|
| Malicious code in template files | Templates are static files. No code execution during application. Users can inspect the template repo before using it. |
| Supply chain attack via index poisoning | Official index is from `dotnet/aspire` (trusted). Community indexes are opt-in with user consent. |
| Man-in-the-middle on template fetch | Git clone uses HTTPS. Commit SHAs in cache provide integrity verification. |
| Typosquatting template names | Official templates take priority in resolution. Warnings shown for templates from non-verified sources. |
| Malicious post-generation hooks | No hooks. Templates do not support arbitrary code execution. |
| Sensitive data in templates | Templates are public git repos. No secrets should be in templates. `.gitignore` is respected. |

### Trust Levels

Templates are categorized by trust:

| Level | Source | UX Treatment |
|-------|--------|-------------|
| **Verified** | `dotnet/aspire` and verified partners | No warnings, shown first |
| **Organizational** | User's GitHub org repos | Subtle note about source |
| **Personal** | User's own repos | Subtle note about source |
| **Community** | Any other repo | Warning banner on first use, user must confirm |

### What Templates Cannot Do

This is a critical security property. Template application is purely:
- File copy
- String substitution in filenames and content
- Conditional file inclusion/exclusion

Templates **cannot**:
- Execute arbitrary commands
- Run scripts (pre or post generation)
- Access the network
- Read files outside the template directory
- Modify the user's system configuration
- Install packages or dependencies

If a template needs post-creation setup (e.g., `npm install`, `dotnet restore`), the `postMessages` field can instruct the user, but the CLI does not execute these automatically.

### Integrity & Content Verification

> **TODO: This section requires input from the security team before finalizing.**

Template content is fetched from git repositories over the network, which introduces integrity concerns beyond the basic threat model above. The following areas need security review:

#### Index Integrity

- **Question:** Should `aspire-template-index.json` files support content hashes or signatures to verify that referenced templates haven't been tampered with?
- **Question:** When an index references templates in external repos (`"repo": "https://github.com/contoso/template"`), how do we verify that the external repo is the one the index author intended? Should we pin to commit SHAs in the index?
- **Possible approach:** Index entries could include an optional `sha` field that pins a template to a specific commit. The CLI would verify the fetched content matches.

#### Template Content Integrity

- **Question:** Should we compute and verify checksums of template files after fetch?
- **Question:** Should we support signing of templates or template indexes (e.g., GPG signatures on commits/tags)?
- **Possible approach:** Cache entries are already keyed by repo URL + commit SHA, providing basic integrity. We may want to go further with explicit verification.

#### Index Federation Trust Chain

- **Question:** When the official index includes a community index via `includes`, what trust guarantees do we provide? The community index could change its content at any time.
- **Question:** Should we support depth-limited trust — e.g., the official index can include partner indexes, but partner indexes cannot transitively include further indexes?
- **Possible approach:** Trust could degrade with federation depth. Direct includes from the official index inherit "verified partner" status; deeper levels are treated as "community."

#### Cache Poisoning

- **Question:** If an attacker can write to the local cache directory (`~/.aspire/template-cache/`), they could substitute malicious template content. Should we sign cache entries?
- **Possible approach:** Cache entries could include a manifest with commit SHAs and checksums, verified on read.

#### Audit Trail

- **Question:** Should the CLI log which template was used, from which source, at which commit SHA, when a project is created? This would help with incident response if a template source is later found to be compromised.
- **Possible approach:** Write a `template-provenance.json` to the generated project recording source repo, commit SHA, template name, and timestamp.

## 11. Polyglot Support

Because templates are real Aspire applications, polyglot support is inherent:

### C# Template Example

```
templates/aspire-starter/
├── aspire-template.json
├── AspireStarter.sln
├── AspireStarter.AppHost/
│   ├── Program.cs
│   └── AspireStarter.AppHost.csproj
└── AspireStarter.Web/
    ├── Program.cs
    └── AspireStarter.Web.csproj
```

### TypeScript Template Example

```
templates/aspire-ts-starter/
├── aspire-template.json
├── apphost.ts
├── package.json
├── tsconfig.json
└── services/
    └── api/
        ├── index.ts
        └── package.json
```

### Python Template Example

```
templates/aspire-py-starter/
├── aspire-template.json
├── apphost.py
├── requirements.txt
└── services/
    └── api/
        ├── app.py
        └── requirements.txt
```

The template engine doesn't need to know anything about the language. It operates purely on files and strings.

## 12. Template Authoring Guide

Creating an Aspire template is designed to be trivially easy. Here's the complete workflow:

### The 5-Minute Path: Your Repo IS the Template

If you have a working Aspire application in a git repo, you're 90% of the way to a template:

**Step 1:** Pick a canonical project name. This is the string that will be replaced with the user's project name. For example, if your project is called `ContosoShop`, that's your canonical name.

**Step 2:** Create `aspire-template.json` in the repo root:

```json
{
  "$schema": "https://aka.ms/aspire/template-schema/v1",
  "version": 1,
  "name": "contoso-shop",
  "displayName": "Contoso Shop",
  "description": "A microservices e-commerce application with Aspire.",
  "language": "csharp",
  "variables": {
    "projectName": {
      "displayName": "Project Name",
      "description": "The name for your new application.",
      "type": "string",
      "required": true,
      "defaultValue": "ContosoShop"
    }
  },
  "substitutions": {
    "filenames": {
      "ContosoShop": "{{projectName}}"
    },
    "content": {
      "ContosoShop": "{{projectName}}"
    }
  }
}
```

**Step 3:** Push to GitHub. That's it.

Anyone can now use your template:

```bash
aspire new --template-repo https://github.com/you/contoso-shop --name MyShop
```

### Making It Discoverable

To make your template show up in `aspire new` without requiring `--template-repo`:

**Personal discovery:** Name your repo `aspire-templates` (i.e., `github.com/{you}/aspire-templates`). If the user of `aspire new` has the GitHub CLI installed and is authenticated, your templates automatically appear in their template list.

**Organizational discovery:** Create `github.com/{your-org}/aspire-templates` with an `aspire-template-index.json` that lists templates across your org's repos. Every org member sees these templates automatically.

**Official inclusion:** Submit a PR to `dotnet/aspire` adding your repo to the `includes` section of the official index.

### Multi-Template Repositories

If you maintain multiple templates in one repo, add an `aspire-template-index.json`:

```json
{
  "$schema": "https://aka.ms/aspire/template-index-schema/v1",
  "version": 1,
  "publisher": {
    "name": "Your Team"
  },
  "templates": [
    {
      "name": "basic-api",
      "displayName": "Basic API",
      "description": "A simple API with Aspire.",
      "path": "templates/basic-api",
      "language": "csharp"
    },
    {
      "name": "full-stack",
      "displayName": "Full Stack App",
      "description": "React frontend + .NET API with Aspire.",
      "path": "templates/full-stack",
      "language": "csharp"
    }
  ]
}
```

### Testing Your Template

Since your template is a working Aspire app, you test it by running it:

```bash
# Test the template as an app
cd templates/basic-api
dotnet run --project BasicApi.AppHost

# Test the template engine
aspire new --template-repo . --name TestOutput -o /tmp/test-output
cd /tmp/test-output
dotnet run --project TestOutput.AppHost
```

Both commands should work. The first verifies your app works, the second verifies the substitutions produce a working app.

## 13. Open Questions

These items need further discussion before finalizing:

1. **Version pinning:** Should templates support version tags (git tags)? Should users be able to pin to a specific version of a template?

2. **Template inheritance/composition:** Should templates be able to extend or compose other templates? (e.g., "start with aspire-starter, add Azure Service Bus")

3. **Offline story:** What happens when the user has no network access and no cache? Should we ship a minimal set of embedded templates?

4. **Template validation:** Should we provide a `aspire template validate` command for template authors? What validation rules?

5. **Rate limiting:** GitHub API rate limits could affect template discovery for unauthenticated users. How do we handle this gracefully?

6. **Private repos:** Should we support templates from private git repos? What authentication flows?

7. **Template updates:** When a user has an existing project created from a template, should we support updating/diffing against newer template versions?

8. **Monorepo templates:** For templates that contain multiple solutions/projects, should we support selective sub-template application?

## 14. Future Considerations

These are explicitly out of scope for v1 but worth tracking:

- **Template marketplace:** A web UI for discovering and previewing templates
- **Template testing framework:** Automated testing for template authors to verify their templates work
- **IDE integration:** VS/VS Code extensions that surface git-based templates in the new project dialog
- **Template analytics:** Opt-in usage tracking to help template authors understand adoption
- **OCI registry support:** Distributing templates as OCI artifacts for air-gapped environments
- **Template generators:** Executable templates for advanced scenarios (with appropriate security guardrails)

## 15. Implementation Plan

This section outlines the incremental implementation strategy. The approach is command-first: stub out the `aspire template` command tree early, then use those commands as the primary interface for developing and testing the underlying infrastructure.

### Phase 1: Foundation — Command Tree & Feature Flag

**Goal:** Get `aspire template *` commands visible and responding (with stub/mock output) behind a feature flag.

**Work items:**

1. **Add feature flag** `gitTemplatesEnabled` to `KnownFeatures.cs` (default: `false`)
2. **Add config keys** to `KnownFeatures.cs` or equivalent:
   - `templates.indexes.*` — named index sources (dictionary pattern, e.g. `templates.indexes.default`, `templates.indexes.contoso`)
   - `templates.defaultBranch`
   - `templates.cacheTtlMinutes`
   - `templates.enablePersonalDiscovery`
   - `templates.enableOrgDiscovery`
3. **Create `TemplateCommand.cs`** — parent command for the `aspire template` group (follows `ConfigCommand` pattern)
4. **Create subcommand stubs:**
   - `TemplateListCommand.cs` — stub that outputs "not yet implemented"
   - `TemplateSearchCommand.cs` — stub with `<keyword>` argument
   - `TemplateRefreshCommand.cs` — stub
   - `TemplateNewCommand.cs` — stub that scaffolds `aspire-template.json`
   - `TemplateNewIndexCommand.cs` — stub that scaffolds `aspire-template-index.json`
5. **Register in `Program.cs`** — add `TemplateCommand` to root command, gated on feature flag
6. **Tests** — basic command parsing tests for the new command tree

**Key files:**
```
src/Aspire.Cli/
├── Commands/
│   └── Template/
│       ├── TemplateCommand.cs              # Parent: aspire template
│       ├── TemplateListCommand.cs          # aspire template list
│       ├── TemplateSearchCommand.cs        # aspire template search <keyword>
│       ├── TemplateRefreshCommand.cs       # aspire template refresh
│       ├── TemplateNewCommand.cs           # aspire template new [path]
│       └── TemplateNewIndexCommand.cs      # aspire template new-index [path]
├── KnownFeatures.cs                        # + gitTemplatesEnabled flag
└── Configuration/
    └── (config keys added)
```

### Phase 2: Schema & Scaffolding

**Goal:** `aspire template new` and `aspire template new-index` produce valid, well-formed JSON files.

**Work items:**

1. **Define C# models** for `aspire-template.json` and `aspire-template-index.json` schemas
   - `GitTemplateManifest` — deserialized `aspire-template.json`
   - `GitTemplateIndex` — deserialized `aspire-template-index.json`
   - `GitTemplateVariable`, `GitTemplateSubstitutions`, etc.
2. **Implement `aspire template new`** — interactive prompts (via `IInteractionService`) to collect template name, canonical project name, etc. Writes `aspire-template.json`.
3. **Implement `aspire template new-index`** — interactive prompts for publisher info. Writes `aspire-template-index.json`.
4. **JSON schema files** — publish schemas at `https://aka.ms/aspire/template-schema/v1` and `https://aka.ms/aspire/template-index-schema/v1` (or embed for offline use).

**Key files:**
```
src/Aspire.Cli/
└── Templating/
    └── Git/
        ├── GitTemplateManifest.cs           # aspire-template.json model
        ├── GitTemplateIndex.cs              # aspire-template-index.json model
        ├── GitTemplateVariable.cs           # Variable definition model
        └── GitTemplateSubstitutions.cs      # Substitution rules model
```

### Phase 3: Index Resolution & Caching

**Goal:** `aspire template list` and `aspire template search` return real data from git-hosted indexes.

**Work items:**

1. **`IGitTemplateIndexService`** — service that fetches, parses, and caches template indexes
   - Shallow sparse clone of repo root to get index file
   - Index graph walking with cycle detection (track visited repo URLs) and depth limit (5)
   - Cache layer with configurable TTL
   - Background refresh on CLI startup
2. **Default index resolution** — fetch from `dotnet/aspire` repo at configured branch
3. **Personal/org discovery** — use `gh` CLI to get authenticated user info and org list, check for `{name}/aspire-templates` repos
4. **Additional indexes** — parse `templates.additionalIndexes` config value
5. **Implement `aspire template list`** — render grouped template table via `IInteractionService.DisplayRenderable()`
6. **Implement `aspire template search`** — filter templates by keyword match on name, description, tags
7. **Implement `aspire template refresh`** — invalidate cache and re-fetch all indexes

**Key files:**
```
src/Aspire.Cli/
└── Templating/
    └── Git/
        ├── IGitTemplateIndexService.cs      # Index fetching & caching
        ├── GitTemplateIndexService.cs        # Implementation
        ├── GitTemplateCache.cs              # Local cache management
        └── GitTemplateSource.cs             # Represents a template source (official, personal, org, explicit)
```

### Phase 4: Template Application Engine

**Goal:** `aspire new --template-repo <url>` creates a real project from a git-based template.

**Work items:**

1. **`IGitTemplateEngine`** — service that applies a template:
   - Clone template content (shallow + sparse checkout of template path)
   - Read `aspire-template.json` manifest
   - Prompt for variables
   - Copy files with exclusions (manifest, `.git/`, `.github/`, conditional files)
   - Apply filename substitutions (deepest-first)
   - Apply content substitutions (skip binary files)
   - Display post-creation messages
2. **Variable expression evaluator** — handles `{{var}}`, `{{var | lowercase}}`, etc.
3. **Binary file detection** — extension allowlist + null-byte sniffing
4. **Implement `--template-repo` flag on `aspire new`**

**Key files:**
```
src/Aspire.Cli/
└── Templating/
    └── Git/
        ├── IGitTemplateEngine.cs             # Template application interface
        ├── GitTemplateEngine.cs              # Implementation
        ├── TemplateExpressionEvaluator.cs    # {{var | filter}} evaluation
        └── BinaryFileDetector.cs             # Binary file detection
```

### Phase 5: GitTemplateFactory Integration

**Goal:** Git-based templates appear in `aspire new` alongside built-in templates.

**Work items:**

1. **`GitTemplateFactory : ITemplateFactory`** — returns `ITemplate` instances backed by git-hosted templates
   - Uses `IGitTemplateIndexService` for discovery
   - Each template is a `GitTemplate : ITemplate` that delegates to `IGitTemplateEngine`
2. **Register in `Program.cs`** via `TryAddEnumerable` (same pattern as `DotNetTemplateFactory`)
3. **Template deduplication** — when both factories provide a template with the same name, use priority (official git > dotnet new > community git)
4. **Interactive selection** — `aspire new` without arguments shows all templates grouped by source

**Key files:**
```
src/Aspire.Cli/
└── Templating/
    └── Git/
        ├── GitTemplateFactory.cs            # ITemplateFactory implementation
        └── GitTemplate.cs                   # ITemplate backed by git template
```

### Phase 6: Polish & GA

**Goal:** Production-ready for public use.

**Work items:**

1. **Error handling** — graceful degradation when git/network unavailable
2. **Progress indicators** — show clone/fetch progress via `IInteractionService`
3. **Telemetry** — template usage events (template name, source, language — no PII)
4. **Documentation** — user-facing docs for template authoring and `aspire template` commands
5. **Remove feature flag** — flip `gitTemplatesEnabled` default to `true`
6. **Publish official template index** — add `templates/aspire-template-index.json` to the Aspire repo

## Appendix A: Research & Prior Art

### .NET Template Engine (`dotnet new`)

The .NET template engine already embraces the "runnable project" philosophy. From the [template.json reference](https://github.com/dotnet/templating/wiki/Reference-for-template.json):

> A "runnable project" is a project that can be executed as a normal project can. Instead of updating your source files to be tokenized you define replacements, and other processing, mostly in an external file, the `template.json` file.

Our git-based system builds on this proven concept. `dotnet new` continues to serve the .NET ecosystem with NuGet-packaged templates and is not replaced or deprecated by this spec. `aspire new` adds git-based distribution and federated discovery as a complementary layer for Aspire CLI users.

**Comparison:**

| Aspect | `dotnet new` Templates | Aspire Git Templates (`aspire new`) |
|--------|----------------------|---------------------|
| **Distribution** | NuGet packages | Git repositories |
| **Discovery** | NuGet feeds, `dotnet new search` | Federated git-based indexes |
| **Manifest** | `.template.config/template.json` | `aspire-template.json` (repo root) |
| **Substitution** | Symbol-based with generators, computed symbols, conditional preprocessing | Simple variable substitution with filters |
| **Post-actions** | Executable (restore, open IDE, run script) | Messages only (no code execution) |
| **Language scope** | .NET languages | Any language |
| **Authoring** | Create template, package as NuGet, publish to feed | Add JSON file to repo, push to git |
| **GUID handling** | Automatic GUID regeneration across formats | Not yet specified (see Open Questions) |
| **Coexistence** | Continues as-is | Additive — does not replace `dotnet new` |

**Design rationale for divergence:**

We intentionally keep the substitution model simpler than `dotnet new`'s full symbol/generator system. The .NET template engine supports computed symbols, derived values, and conditional preprocessing — powerful features but ones that add complexity and .NET-specific concepts. Our system favors simplicity and transparency: what you see in the repo is what you get, with straightforward name replacements. If complex project generation is needed, build a tool that generates the output directly.

### Cookiecutter

[Cookiecutter](https://cookiecutter.readthedocs.io/) is a popular cross-language project templating tool. Templates are directories with `{{cookiecutter.variable}}` placeholders in filenames and content, plus a `cookiecutter.json` defining variables and defaults.

**What we borrow:**
- The concept that a template is a directory of real files with variable placeholders
- JSON manifest for variable definitions with defaults
- Git repository as distribution mechanism (`cookiecutter gh:user/repo`)

**Where we differ:**
- Cookiecutter uses Jinja2 templating (powerful but complex); we use simple find-and-replace with filters
- Cookiecutter supports pre/post-generation hooks (Python/shell scripts); we explicitly forbid code execution for security
- Cookiecutter has no federated discovery; we have index walking

### GitHub Template Repositories

GitHub's [template repositories](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-template-repository) allow creating new repos from a template with one click. They're simple (just copy files) but have no variable substitution, no parameterization, and no CLI integration.

**What we borrow:**
- The idea that a git repo IS the template
- Zero-friction publishing (just mark a repo as a template)

**Where we differ:**
- We add variable substitution for project name personalization
- We add federated discovery across multiple sources
- We integrate with the Aspire CLI rather than GitHub's web UI

### Yeoman

[Yeoman](https://yeoman.io/) generators are npm packages that programmatically scaffold projects. They're extremely flexible but require JavaScript knowledge to author and npm infrastructure to distribute.

**What we learn:**
- Yeoman's power comes at the cost of high authoring complexity — most teams never create generators
- The npm distribution model creates friction for non-JavaScript ecosystems
- Our approach inverts this: minimal authoring effort, git-native distribution

### Rollout Plan

The git-based template system will be introduced alongside `dotnet new`:

1. **Phase 1 (this spec):** Implement git-based template resolution in `aspire new`. The `--template-repo` flag enables explicit use; `aspire new` also shows git-sourced templates alongside the existing built-in templates.
2. **Phase 2:** Publish official Aspire templates to `dotnet/aspire` as git-based templates. These are the same templates available via `dotnet new`, also discoverable through the git-based system.
3. **Phase 3:** Community and partner templates begin appearing in federated indexes. `aspire new` becomes the recommended way to discover Aspire templates.

### For `dotnet new` Template Authors

Teams currently creating `dotnet new` templates can additionally make them available as git-based templates:

1. Take your existing template output (what `dotnet new` generates)
2. Replace parameter placeholders with the canonical project name
3. Add `aspire-template.json` with variable definitions
4. Push to a git repo
5. Optionally, register in a template index

This is additive — the `dotnet new` template continues to work as before.
