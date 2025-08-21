# Aspire

[![Tests](https://github.com/dotnet/aspire/actions/workflows/tests.yml/badge.svg?branch=main&event=push)](https://github.com/dotnet/aspire/actions/workflows/tests.yml)
[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status%2Fdotnet%2Faspire%2Fdotnet.aspire?branchName=main)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=274&branchName=main)
[![Help Wanted](https://img.shields.io/github/issues/dotnet/aspire/help%20wanted?style=flat&color=%24EC820&label=help%20wanted)](https://github.com/dotnet/aspire/labels/help%20wanted)
[![Good First Issue](https://img.shields.io/github/issues/dotnet/aspire/good%20first%20issue?style=flat&color=%24EC820&label=good%20first%20issue)](https://github.com/dotnet/aspire/labels/good%20first%20issue)
[![Discord](https://img.shields.io/discord/1361488941836140614?style=flat&logo=discord&logoColor=white&label=Join%20our%20Discord&labelColor=512bd4&color=cyan)](https://discord.gg/raNPcaaSj8)

## What is Aspire?

Aspire provides tools, templates, and packages for building observable, production-ready distributed apps. At the center is the app model—a code-first, single source of truth that defines your app's services, resources, and connections.

Aspire gives you a unified toolchain: launch and debug your entire app locally with one command, then deploy anywhere—Kubernetes, the cloud, or your own servers—using the same composition.

## Useful links

- [Aspire overview and documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Aspire samples repository](https://github.com/dotnet/aspire-samples)
- [Dogfooding pull requests](docs/dogfooding-pull-requests.md) - Test changes from specific pull requests locally

## Getting started

### Install the Aspire CLI

To install the latest released version of the Aspire CLI:

On Windows:

```powershell
iex "& { $(irm https://aspire.dev/install.ps1) }"
```

On Linux or macOS:

```sh
curl -sSL https://aspire.dev/install.sh | bash
```

> [!NOTE]
> If you want to use the latest daily builds instead of the released version, follow the instructions in [docs/using-latest-daily.md](docs/using-latest-daily.md).

## What is in this repo?

The Aspire application host, dashboard, service discovery infrastructure, and all Aspire integrations. It also contains the project templates.

## How can I contribute?

We welcome contributions! Many people all over the world have helped make Aspire better.

Follow instructions in [docs/contributing.md](docs/contributing.md) for working in the code in the repository.

## Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these instructions in this repo's [Security doc](SECURITY.md).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

### Note on containers used by Aspire resource and client integrations

The .NET team cannot evaluate the underlying third-party containers for which we have API support for suitability for specific customer requirements.

You should evaluate whichever containers you chose to compose and automate with Aspire to ensure they meet your, your employers or your government’s requirements on security and safety as well as cryptographic regulations and any other regulatory or corporate standards that may apply to your use of the container.

## .NET Foundation

Aspire is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

* [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
* [ASP.NET Core home](https://docs.microsoft.com/aspnet/core) - the best place to start learning about ASP.NET Core.

This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://www.dotnetfoundation.org/code-of-conduct).

## License

The code in this repo is licensed under the [MIT](LICENSE.TXT) license.
