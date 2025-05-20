# .NET Aspire

[![Tests](https://github.com/dotnet/aspire/actions/workflows/tests.yml/badge.svg?branch=main&event=push)](https://github.com/dotnet/aspire/actions/workflows/tests.yml)
[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status%2Fdotnet%2Faspire%2Fdotnet.aspire?branchName=main)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=274&branchName=main)
[![Help Wanted](https://img.shields.io/github/issues/dotnet/aspire/help%20wanted?style=flat&color=%24EC820&label=help%20wanted)](https://github.com/dotnet/aspire/labels/help%20wanted)
[![Good First Issue](https://img.shields.io/github/issues/dotnet/aspire/good%20first%20issue?style=flat&color=%24EC820&label=good%20first%20issue)](https://github.com/dotnet/aspire/labels/good%20first%20issue)
[![Discord](https://img.shields.io/discord/732297728826277939?style=flat&logo=discord&logoColor=white&label=Join%20our%20Discord&labelColor=512bd4&color=cyan)](https://discord.com/invite/h87kDAHQgJ)

## What is .NET Aspire?

.NET Aspire is an opinionated, cloud ready stack for building observable, production ready, distributed applications. .NET Aspire is delivered through a collection of NuGet packages that handle specific cloud-native concerns. Cloud-native apps often consist of small, interconnected pieces or microservices rather than a single, monolithic code base. Cloud-native apps generally consume a large number of services, such as databases, messaging, and caching.

.NET Aspire helps with:

[Orchestration](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview?#orchestration): .NET Aspire provides features for running and connecting multi-project applications and their dependencies.

[Integrations](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview?#net-aspire-integrations): .NET Aspire integrations are NuGet packages for commonly used services, such as Redis or Postgres, with standardized interfaces ensuring they connect consistently and seamlessly with your app.

[Tooling](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview?#project-templates-and-tooling): .NET Aspire comes with project templates and tooling experiences for Visual Studio and the dotnet CLI which help you create and interact with .NET Aspire apps.

To learn more, read the full [.NET Aspire overview and documentation](https://learn.microsoft.com/dotnet/aspire/). Samples are available in the [.NET Aspire samples repository](https://github.com/dotnet/aspire-samples). You can find the [eShop sample here](https://github.com/dotnet/eshop) and the [Azure version here](https://github.com/Azure-Samples/eShopOnAzure).

## What is in this repo?

The .NET Aspire application host, dashboard, service discovery infrastructure, and all .NET Aspire integrations. It also contains the project templates.

## Using latest daily builds

Follow instructions in [docs/using-latest-daily.md](docs/using-latest-daily.md) to get started using .NET Aspire with the latest daily build.

## How can I contribute?

We welcome contributions! Many people all over the world have helped make .NET better.

Follow instructions in [docs/contributing.md](docs/contributing.md) for working in the code in the repository.

## Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these instructions in this repo's [Security doc](SECURITY.md).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

### Note on containers used by .NET Aspire resource and client integrations

The .NET team cannot evaluate the underlying third-party containers for which we have API support for suitability for specific customer requirements.

You should evaluate whichever containers you chose to compose and automate with Aspire to ensure they meet your, your employers or your government’s requirements on security and safety as well as cryptographic regulations and any other regulatory or corporate standards that may apply to your use of the container.

## .NET Foundation

.NET Aspire is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

* [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
* [ASP.NET Core home](https://docs.microsoft.com/aspnet/core) - the best place to start learning about ASP.NET Core.

This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://www.dotnetfoundation.org/code-of-conduct).

## License

The code in this repo is licensed under the [MIT](LICENSE.TXT) license.
