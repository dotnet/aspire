# .NET Aspire 13.0 Milestone Items

Generated: 2025-11-03T08:12:34.636Z
Total items: 693 (Issues: 151, PRs: 542)

| Number | Type | Title | Labels |
|---|---|---|---|
| #12601 | Issue | Make state changes on IReportingStep and IReportingTask idempotent | area-pipelines |
| #12593 | Issue | When the pipeline fails its hard to see errors | area-pipelines, 💅🏽 polish |
| #12573 | PR | [WIP] Disable DockerComposePublisherTests.PublishAsync test with ActiveIssue |  |
| #12572 | Issue | Running the new python starter template: Dashboard crashes | area-orchestrator |
| #12571 | PR | [WIP] Add custom agent markdown file for testing |  |
| #12557 | PR | [release/13.0] Fix MCP endpoint with redirect HTTPS | area-dashboard, Servicing-approved, Re-opened Github-Action PR |
| #12556 | PR | Fix MCP endpoint with redirect HTTPS | area-dashboard |
| #12550 | PR | [release/13.0] Update to FluentUI 4.13.1 | area-dashboard, Servicing-approved, Re-opened Github-Action PR |
| #12549 | PR | [release/13.0] Improve flaky dashboard integration test | area-dashboard, Servicing-approved, Re-opened Github-Action PR |
| #12547 | PR | Skip playground projects in build-packages workflow |  |
| #12542 | PR | Remove NPM install from frontend configuration |  |
| #12540 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12538 | PR | Refactor AddNodeApp | breaking-change |
| #12537 | PR | Replace apt-get with npm for Azure Functions Core Tools installation |  |
| #12534 | PR | Improve flaky dashboard integration test | area-dashboard |
| #12532 | PR | Update to FluentUI 4.13.1 | area-dashboard |
| #12531 | Issue | Python scripts don't work | python |
| #12530 | PR | Refactor NodeJs Integration |  |
| #12528 | PR | Add README.md for the Aspire.Hosting.Azure.AppService package | community-contribution |
| #12525 | PR | Remove fake passwords from ExpressionResolverTests test data |  |
| #12521 | PR | Allow HostUrl to remap both address and port |  |
| #12514 | PR | [TESTING] Throw NotSupportedException for resources that are not supported in AppServiceEnvironment | community-contribution |
| #12508 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12507 | PR | Add WORKDIR /app to PublishWithStaticFiles Dockerfile generation |  |
| #12502 | PR | Add UvicornAppResource deriving from PythonAppResource |  |
| #12501 | PR | Change default behavior of WithNpm/WithYarn/WithPnpm to install packages automatically |  |
| #12500 | PR | Target net10.0 in client integrations |  |
| #12499 | PR | Remove fake steps from publish pipeline |  |
| #12494 | PR | Stablize Aspire.Hosting.Azure.AppService |  |
| #12492 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2827580 |  |
| #12490 | PR | Set OptimizationPreference=Size on Aspire.Cli |  |
| #12488 | PR | add interpreter path and module properties to IDE execution spec |  |
| #12487 | PR | remove old aspirewithpython ref from slnx |  |
| #12486 | PR | Revert "Update dependencies from https://github.com/dotnet/arcade build 20251024.1 (#12399)" |  |
| #12480 | Issue | Remove "fake steps" from publish | area-deployment |
| #12477 | PR | localhive scripts: avoid building unrelated projects like tests and playground apps |  |
| #12476 | PR | Fix the logging pipeline so that logger provider filtering works |  |
| #12474 | PR | MCP result improvements: Provide links to resources, traces, logs. Shorten resource names. | area-dashboard |
| #12473 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12468 | PR | Update deployment-docker test scenario to use `aspire deploy` command |  |
| #12467 | PR | Display VS Code tab first in MCP dialog | area-dashboard |
| #12466 | PR | Make *.localhost resource endpoint URLs the primary endpoint URL |  |
| #12465 | Issue | URLs added to resources for *.dev.locahost change perceived behavior of WithUrlForEndpoint | area-app-model |
| #12461 | PR | Improve AI Foundry Local models detection |  |
| #12460 | PR | Fix the path in the python template |  |
| #12457 | PR | Update package dependencies |  |
| #12456 | PR | Support debugging python modules, add flask, uvicorn debugging support, add DCP logging util |  |
| #12455 | PR | Support aspire deploy for Docker Compose |  |
| #12454 | PR | Aspire CLI global tool target net8 |  |
| #12453 | PR | Fix OpenApi dependency version to align with the rest |  |
| #12452 | PR | Fix LoadDeploymentState to check Pipeline:ClearCache instead of Publishing:ClearCache |  |
| #12451 | Issue | Fix clear cache handling for deployment state loading | area-deployment |
| #12450 | PR | Remove PublishingCallbackAnnotation and PublishingContext |  |
| #12449 | PR | Make Azure.AIFoundry preview again |  |
| #12445 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2826408 |  |
| #12444 | PR | Remove exception message assertion from PerTestFrameworkTemplatesTests |  |
| #12443 | PR | Disable failing PerTestFrameworkTemplatesTests in outer loop CI |  |
| #12442 | PR | Replace ".NET Aspire" with "Aspire" in user-facing strings |  |
| #12441 | PR | Add eshop-update scenario for testing aspire update on real-world applications | area-engineering-systems |
| #12440 | PR | Update package descriptions: Replace ".NET Aspire" with "Aspire" |  |
| #12438 | PR | Disable resource-related MCP tools when resource service is not configured |  |
| #12435 | PR | Minor AspireMcpTools clean up | area-dashboard |
| #12434 | PR | Move hardcoded text in McpServerDialog to resource files |  |
| #12431 | Issue | Move MCP dialog text into resource files | area-dashboard, ai |
| #12430 | Issue | Disable resource related MCP tools when there is no resource service specified | area-dashboard, ai |
| #12429 | Issue | Change MCP VS Code workaround instructions to configure individual MCP endpoint | area-dashboard, ai |
| #12427 | PR | Add download progress indicator for .NET SDK installation |  |
| #12424 | PR | Add deployment-docker test scenario for Docker Compose workflow |  |
| #12423 | PR | Add copilot instructions for test scenario prompt authoring |  |
| #12422 | PR | Expose option for *.dev.localhost URLs during local dev in `aspire new` |  |
| #12421 | PR | Remove most of ASPIRECOMPUTE001 |  |
| #12420 | PR | Remove ASPIREHOSTINGPYTHON001 |  |
| #12419 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12418 | PR | Remove PublishingContext and PublishingCallbackAnnotation from public API |  |
| #12417 | PR | Make the following libraries stable: |  |
| #12416 | PR | Rename ASPIREPUBLISHERS001 to sequential ASPIREPIPELINES001-003 diagnostic codes |  |
| #12415 | Issue | Fix up Experimental diagnostic codes in pipelines area | area-pipelines |
| #12414 | PR | Set default OutputPath and add IntermediateOutputPath at PipelineContext level |  |
| #12412 | Issue | Default output path set from CLI not respected during aspire deploy | area-pipelines |
| #12411 | PR | Update default schema version |  |
| #12408 | PR | Return message at root for api service in starter template |  |
| #12405 | Issue | API endpoint in aspire-starter should return something when invoked on / | area-templates |
| #12404 | PR | Follow up feedback for #12121 |  |
| #12399 | PR | [main] Update dependencies from dotnet/arcade |  |
| #12398 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2826120 |  |
| #12396 | PR | Remove [ActiveIssue] attributes for resolved issue #11728 from AzureDeployerTests |  |
| #12393 | PR | Remove agent PR polling from test-scenario workflow | area-engineering-systems |
| #12390 | PR | Rename smoke-test to smoke-test-dotnet and add smoke-test-python scenario | area-engineering-systems |
| #12388 | Issue | Pipeline polish and cleanup | area-deployment |
| #12387 | PR | Add ASPIRE_PLAYGROUND environment variable to force interactive mode with ANSI and color support |  |
| #12385 | PR | Decentralize build/push/deploy pipeline logic to resources and deployment targets |  |
| #12383 | PR | Add X509Certificate2Extensions.cs to Helix payload for Aspire.Playground.Tests |  |
| #12380 | PR | Fixed FormatBicepExpression to support string and `FormattableString` types, improving flexibility. |  |
| #12379 | PR | Add --list-steps flag to aspire do command |  |
| #12377 | PR | Add `aspire deploy --list-steps` flag to preview pipeline execution |  |
| #12376 | Issue | Add support for `aspire do --list-steps` flag | area-deployment |
| #12375 | PR | Add DoCommand as general pipeline entry point |  |
| #12373 | PR | Add MAUI hosting package icon |  |
| #12371 | PR | Remove dotnet10-workloads NuGet feed |  |
| #12369 | PR | Add comprehensive smoke-test scenario for PR build validation |  |
| #12368 | Issue | Can't deploy app that references postgres url | area-deployment |
| #12366 | PR | Change issue assignee from 'copilot' to 'copilot-swe-agent' |  |
| #12365 | PR | Handle file locks gracefully during CLI self-update on Windows | area-cli |
| #12363 | PR | [Automated] Update AI Foundry Models | area-integrations, NO-MERGE, area-engineering-systems, Re-opened Github-Action PR |
| #12362 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12361 | PR | Enable universal support for container-to-host communication | breaking-change |
| #12360 | PR | Add Aspire.Azure.Data.AppConfiguration client integration package |  |
| #12359 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12358 | PR | Rework certificate environment and argument config to be more idiomatic |  |
| #12355 | PR | Add ContainerFilesDestinationAnnotation support to ProjectResource |  |
| #12353 | PR | Refactor Python template |  |
| #12352 | PR | Refactor pipeline: Remove RequiredBySteps property and model everything via DependsOn |  |
| #12351 | PR | [WIP] Remove RequiredBy property and update transitive dependencies |  |
| #12350 | PR | Support modeling publish as a pipeline step |  |
| #12349 | PR | Updating branding for nuget hosting packages |  |
| #12347 | PR | Fix failing AddViteAppTests.VerifyDefaultDockerfile on Helix using TempDirectory |  |
| #12342 | PR | Add Aspire.Hosting.Maui (.NET MAUI) Mac Catalyst integration |  |
| #12341 | PR | Fix flaky test ExecuteAsync_WhenStepFails_PipelineLoggerIsCleanedUp |  |
| #12340 | PR | Decentralize build/push/deploy pipeline logic to resources and deployment targets |  |
| #12337 | PR | Regenerate the lock file and remove ado feeds |  |
| #12336 | PR | Fix thread safety issue in DeploymentStateManager |  |
| #12335 | PR | Initialize pipeline with deploy step and decentralize bicep resource provisioning |  |
| #12334 | Issue | aspire update --self fails on windows | area-cli |
| #12333 | PR | update extension readme and daily doc |  |
| #12331 | PR | Add --tag flag to aspire deploy command to filter pipeline steps by tag |  |
| #12330 | PR | [Only for testing] Appsvc existingai | community-contribution |
| #12327 | PR | Allow AzureEnvironmentResource to delegate to compute resources' build steps |  |
| #12325 | PR | Refactor WithNpmPackageManager to WithNpm with install parameter |  |
| #12324 | PR | Emit separate pipeline steps for each AzureBicepResource and compute resource in AzureEnvironmentResource |  |
| #12323 | PR | Cleanup logging when we don't apply certs |  |
| #12320 | PR | Make PipelineConfigurationContext.StepToResourceMap nullable for type safety |  |
| #12319 | PR | follow up to 12285 |  |
| #12318 | Issue | Add command to open Aspire extension settings | area-extension |
| #12317 | PR | Detect Node.js version in AddViteApp from project configuration files |  |
| #12314 | Issue | Add `aspire deploy --dry-run` to list steps that can be executed | area-deployment |
| #12312 | PR | Add thread-safe status tracking to pipeline steps |  |
| #12311 | PR | [WIP] Compute resources pipeline steps |  |
| #12310 | PR | Fix DockerComposeEnvironment duplicating image names in .env file |  |
| #12309 | Issue | DockerComposeEnvironment duplicates the image name | ‼️regression-from-last-release, area-deployment, docker-compose |
| #12306 | PR | Update writing-guidelines.md |  |
| #12305 | PR | Add support to enable automatic scaling for App Service Environment | community-contribution |
| #12301 | PR | Quarantine flaky tests in ExtensionInternalCommandTests |  |
| #12299 | Issue | Re-plat aspire publish and aspire deploy on specific step execution | area-deployment |
| #12298 | PR | Clean up deployment logs to reduce verbosity and centralize stack trace filtering with DI-based configuration |  |
| #12297 | PR | [Automated] Update AI Foundry Models | area-integrations, area-engineering-systems, Re-opened Github-Action PR |
| #12296 | PR | Embed dotnet-install scripts as resources in Aspire CLI |  |
| #12295 | Issue | `aspire --help` hangs after displaying help message when running under extension. | needs-author-action, area-extension |
| #12293 | PR | Add support for pipeline step tagging and configuration phase dependency management |  |
| #12292 | PR | Remove singlefileAppHostEnabled feature flag and set minimum SDK to .NET 10 |  |
| #12291 | PR | Ensure devtunnel access policies are correct when updating tunnel |  |
| #12290 | PR | Support annotating pipeline steps with tags |  |
| #12289 | Issue | Support annotating pipeline steps with tags | area-deployment |
| #12288 | Issue | Clean up deployment logs now that we have them | area-deployment |
| #12287 | PR | Support executing single step and its dependencies |  |
| #12285 | PR | Don't send CLI logs to extension, add options for setting CLI path & controlling CLI debug output |  |
| #12284 | PR | Add Aspire.Hosting.Maui (.NET MAUI) Windows integration |  |
| #12283 | PR | Add ViteApp and Npm Package Manager |  |
| #12281 | Issue | Add option to set path to Aspire CLI so that developers do not have to modify their path to debug | area-cli, area-extension |
| #12280 | PR | Fix CliPack build error |  |
| #12279 | PR | Add logging capabilities to the publishing pipeline |  |
| #12271 | Issue | Add the ability to run a specific step via aspire deploy | area-deployment |
| #12270 | PR | Improve concurrency handling in IDeploymentStateManager with optimistic concurrency control |  |
| #12269 | PR | Add shared helper methods for localhost address validation |  |
| #12268 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12267 | PR | Update the templates for .NET 10 |  |
| #12265 | PR | Add support for docker static files |  |
| #12263 | PR | Change VS Code confirmation message to quick pick in interaction service |  |
| #12262 | PR | fade apphost logs, put dashboard/codespaces in green in console |  |
| #12260 | PR | Set default target port for app service endpoints if not specified |  |
| #12259 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2823357 |  |
| #12258 | PR | Add python starter template loc files |  |
| #12257 | PR | Add more logging and support custom certificate trust for .NET projects on Mac/Linux |  |
| #12256 | Issue | improve readme | area-extension |
| #12255 | Issue | improve debugger output | area-extension |
| #12251 | Issue | change vs code confirmation message to a quick pick in interaction service | area-extension |
| #12249 | PR | Update copilot instructions for localization files |  |
| #12248 | PR | Fix race condition in OpenAI client validation causing NullReferenceException |  |
| #12246 | PR | Add category tags to Aspire integration packages for improved NuGet discoverability |  |
| #12245 | Issue | Introduce category NuGet `<PackageTags>` | area-integrations |
| #12243 | PR | Implement logging functionality for publishing activities with log le… |  |
| #12240 | PR | Fix aspire deploy of Python apps when uv.lock doesn't exist |  |
| #12239 | PR | Increase CLI executable size limit for Windows from 20 MB to 25 MB |  |
| #12237 | Issue | aspire deploy of the py-starter app without running first fails | area-integrations, python |
| #12236 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2821428 |  |
| #12235 | PR | Add cancellation token to `aspire add` command |  |
| #12234 | PR | Allow configuring the dev cert trust via environment variable as well as DistributedApplicationOptions |  |
| #12233 | PR | Bump the npm_and_yarn group across 3 directories with 1 update | community-contribution, javascript, dependencies |
| #12232 | PR | Bump the npm_and_yarn group across 2 directories with 1 update | community-contribution, javascript, dependencies |
| #12231 | PR | Fix race condition when saving deployment state during concurrent Azure provisioning |  |
| #12230 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2820765 |  |
| #12228 | PR | Fix FormatException in ContainerRuntimeBase exception message templates |  |
| #12226 | PR | Group Parameters under collapsible parent row in Resources table |  |
| #12225 | Issue | [EPIC] `aspire deploy` for v13 | area-deployment |
| #12224 | Issue | VS Code informational message does not go away after aspire add | area-extension |
| #12220 | PR | Flip RabbitMQ.Client v7 and MongoDB.Driver v3 to be the default packages |  |
| #12219 | Issue | Flip RabbitMQ.Client v7 and MongoDB v3 to be the default packages | area-integrations, mongodb, rabbitmq |
| #12218 | PR | Display target environment during `aspire deploy` |  |
| #12217 | Issue | Display target environment during `aspire deploy` | area-deployment |
| #12216 | PR | Render resource formatting in manifest |  |
| #12215 | PR | Enhance python starter template |  |
| #12214 | PR | Automatically save deployment logs to disk for aspire deploy |  |
| #12212 | Issue | Image push intermittently fails with parsing error | area-deployment |
| #12210 | PR | Fix formatting of reporting steps in deployment pipeline |  |
| #12209 | Issue | Fix formatting of various reporting steps in deployment pipeline | area-deployment |
| #12205 | Issue | Parameters are not getting preserved in user secrets when you also have Azure Provisioned resources | area-integrations, azure, ‼️regression-from-last-release |
| #12201 | Issue | Race condition when saving deployment state during Bicep provisioning | area-deployment |
| #12197 | PR | Add fallback parsing support for single-file apphost projects | area-cli |
| #12196 | Issue | `aspire update` needs fallback parsing for single file projects | area-cli |
| #12195 | Issue | Add logging support to deployment steps | area-deployment |
| #12191 | PR | Fix flaky DeployCommandIncludesDeployFlagInArguments test |  |
| #12189 | Issue | python start template debugging doesn't work | area-extension |
| #12188 | PR | Implement automatic .NET SDK installation for Aspire CLI |  |
| #12187 | PR | Clean up version selection display in aspire new and aspire add commands | area-cli |
| #12185 | Issue | Usability improvements to tha azure deployment experience | area-deployment |
| #12182 | PR | Fix incorrect documentation for WithDockerfile() dockerfilePath parameter |  |
| #12176 | PR | Fix error reporting for pipeline step failures in aspire deploy |  |
| #12175 | Issue | Error reporting does not work consistently with aspire deploy | area-deployment |
| #12174 | PR | Remove flaky ExecuteAsync_WithMixOfSuccessfulAndFailingStepsAtSameLevel_ThrowsAggregateException test |  |
| #12173 | PR | Fix PackagePath for aspire-apphost-singlefile template files |  |
| #12172 | PR | Added prompting for tenant to azure config experiences |  |
| #12171 | PR | Remove singlefileAppHostEnabled feature flag and set minimum SDK to .NET 10 |  |
| #12170 | PR | [WIP] Remove singlefileAppHostEnabled feature and set minimum SDK to .NET 10 |  |
| #12168 | PR | Fix template names and visibility for aspire new command |  |
| #12167 | Issue | When the apphost crashes / fails to start the extension run mode hangs on "Connecting to apphost" | area-cli, area-extension |
| #12166 | PR | Fix line truncation in ConsoleActivityLogger by setting unlimited width |  |
| #12165 | PR | Add async PipelineStepFactoryContext and WithPipelineStepFactory extension methods |  |
| #12164 | Issue | Fix Issues for New Aspire Templates |  |
| #12163 | Issue | Promote Python APIs to Stable in Aspire Templates | python |
| #12162 | Issue | Remove Dockerfile from Python/Javascript Aspire Templates Using Pipeline Steps | area-integrations, python |
| #12160 | PR | Remove flaky pipeline tests that rely on race conditions |  |
| #12159 | PR | [Automated] Update Playground Manifests | area-app-model, area-engineering-systems, Re-opened Github-Action PR |
| #12158 | PR | Fix Docker image tag parsing error when environment names contain uppercase or special characters |  |
| #12153 | PR | Fix deferred execution bug in CreateSnapshotableResourcesAsync causing flaky test failures |  |
| #12152 | PR | Fix timeout handling in DcpHostNotificationTests to prevent test hangs |  |
| #12151 | PR | Remove flaky tests incompatible with pipeline early-exit cancellation behavior |  |
| #12150 | PR | Update test-scenario workflow to create issues instead of agent tasks |  |
| #12149 | PR | [Automated] Update AI Foundry Models | area-integrations, area-engineering-systems, Re-opened Github-Action PR |
| #12148 | PR | Add dashboard MCP server | area-dashboard |
| #12147 | PR | Need to reauth. |  |
| #12146 | PR | Add Playwright dependencies caching to reduce CI test times |  |
| #12145 | PR | Temporarily disable pull_request triggers in outerloop & quarantine workflows | area-engineering-systems |
| #12144 | PR | Update CLI version and target repo. |  |
| #12142 | PR | [CI] Outerloop/Quarantined workflows - build only the required bits |  |
| #12141 | PR | Support simplified service url ENV format |  |
| #12137 | PR | Rename AcitivityReporter and auto-create publishing steps | breaking-change |
| #12136 | PR | Add aspire-py-starter template for Python and JavaScript applications |  |
| #12135 | PR | Add ICliHostEnvironment service and --non-interactive flag for clean CI output |  |
| #12134 | Issue | Rendering issues in CI environments | area-deployment |
| #12132 | PR | Add TestContext.Current.CancellationToken support to IntegrationServicesTests |  |
| #12131 | PR | Remove Traces from the Python OTel Cert variable |  |
| #12130 | PR | Add markdown rendering support for publishing activity messages |  |
| #12128 | PR | Use PathSha for deployment state and ProjectNameSha for Azure Functions |  |
| #12127 | PR | Hide obsolete model enum members | breaking-change |
| #12126 | PR | [Automated] Update Playground Manifests | area-app-model, area-engineering-systems, Re-opened Github-Action PR |
| #12125 | Issue | Add integration is slower in extension than CLI | area-extension |
| #12121 | PR | Create Copilot instruction files for hosting and client integration README.md documentation |  |
| #12120 | PR | Add Azure.Data.AppConfiguration activity source to telemetry tracking |  |
| #12118 | PR | Add `aspire update --self` command for CLI self-updates |  |
| #12117 | PR | Add operator role assignment support for Azure resources |  |
| #12116 | PR | Add ASPIRE_INTERACTIVITY_ENABLED configuration and --non-interactive flag to control IInteractionService availability |  |
| #12113 | PR | Add DefaultTimeout to test completion points in WithUrlsTests |  |
| #12112 | PR | Add WithRefreshKey API to simplify Azure App Configuration refresh configuration |  |
| #12111 | PR | Implement pipeline step cancellation on failure |  |
| #12110 | PR | [Azure App Configuration] - Stablize Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration  | community-contribution |
| #12109 | PR | Support arguments in Python Dockerfile ENTRYPOINT generation |  |
| #12108 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2820656 |  |
| #12107 | PR | Improve AI models enum |  |
| #12105 | PR | Add README to Aspire.Hosting.Azure.AppService |  |
| #12104 | PR | Allow continuing on individual file copy errors in WithContainerFiles |  |
| #12103 | PR | Add new `System` and `None` scopes for certificate trust mode, use `System` by default for Python |  |
| #12102 | Issue | Deployment state is based on name of the project, not location | area-deployment |
| #12099 | PR | Implement v3 auto-detection test splitting infrastructure |  |
| #12098 | PR | Remove platform-specific skip condition for ActiveIssue #11728 tests |  |
| #12096 | PR | Make PublishAsDockerFile overloads idempotent |  |
| #12095 | PR | Add step summary, icons, and link detection to PublishCommandBase from PR #11976 |  |
| #12094 | Issue | Support parameter encoding in manifest files | area-azd |
| #12093 | PR | [Automated] Update Playground Manifests | area-app-model, area-engineering-systems, Re-opened Github-Action PR |
| #12092 | PR | Delete empty IInteractionService.cs file from Utils directory |  |
| #12091 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2818042 |  |
| #12090 | Issue | The `IInteractionService.IsAvailable` bool value should be smarter. | area-app-model |
| #12088 | PR | Added check for single exposed port and handle startup command and target port in app service | community-contribution |
| #12087 | PR | Support for .fsproj, .vbproj project files next to .csproj | community-contribution |
| #12085 | PR | Set PYTHONUTF8=1 environment variable for Python apps on Windows in Run mode |  |
| #12082 | PR | Move encoding to ReferenceExpression |  |
| #12081 | Issue | Failed apphost build doesn't clear notification | area-extension |
| #12080 | PR | Add AGENT_TEST.md to verify CLI agent functionality |  |
| #12078 | PR | Replace TestModuleInitializer with ITestPipelineStartup to fix type conflict |  |
| #12077 | PR | Missed refactoring from dynamic input | area-app-model |
| #12075 | PR | Reduce test duplication by creating FluentUISetupHelpers with reusable methods |  |
| #12074 | PR | [main] Update dependencies from microsoft/usvc-apiserver |  |
| #12073 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2817235 |  |
| #12071 | PR | Update KubernetesClient to v18.0.5 and fix breaking changes | area-orchestrator |
| #12070 | Issue | Refactor expression formatting | area-app-model |
| #12069 | Issue | Model service connections like connection properties | area-polyglot |
| #12068 | PR | add extension to get-aspire-cli script, don't install by default | area-cli, area-extension |
| #12067 | PR | Move Verify test initialization to Aspire.TestUtilities to fix duplicate type conflicts |  |
| #12062 | Issue | Support python modules in VS code extension + formalize in ide spec | area-extension |
| #12061 | PR | Updating dependencies to latest .NET Servicing |  |
| #12060 | PR | zip extension vsix in publishing step |  |
| #12059 | PR | Improve pipeline concurrency with readiness-based scheduler |  |
| #12057 | PR | [Automated] Update Playground Manifests | area-app-model, area-engineering-systems, Re-opened Github-Action PR |
| #12055 | PR | Add GitHub workflow triggered by /test-scenario PR comments with scenario-based prompts |  |
| #12052 | PR | Use directory name as dashboard application name for file-based app hosts |  |
| #12050 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2817056 | localization |
| #12047 | PR | Add support for updating single-file app hosts in the update command | area-cli |
| #12046 | PR | Filter outerloop and quarantined tests to single project on PRs (all OSes) |  |
| #12044 | PR | Update to FluentUI 4.13.0 | area-dashboard |
| #12043 | PR | Refactor Python application entrypoint handling and add support for s… | area-integrations |
| #12042 | PR | Remove --ci argument from specialized-test-runner.yml to prevent disk space issues | area-engineering-systems |
| #12041 | PR | Remove ExtensionUtils.IsExtensionHost and simplify WithVSCodeDebugSupport |  |
| #12040 | PR | Fix duplicate meters in metrics tree | area-dashboard |
| #12039 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2816277 | area-dashboard |
| #12038 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2816217 | area-dashboard |
| #12037 | PR | Move Oracle.EntityFrameworkCore tests to outerloop with diagnostic messages | testing ☑️, oracle |
| #12035 | PR | update extension publish path to match signing path | needs-area-label |
| #12034 | Issue | In ExtensionHelper.IsExtensionHost, check for more than just the presence of DEBUG_SESSION_INFO | area-cli, area-extension |
| #12033 | PR | Reactor and add XML comments to dynamic input API | area-app-model |
| #12031 | PR | Implement custom Foundry Local models list. | area-app-model |
| #12030 | PR | Exclude the new CertificateAuthorityCollectionResource from the manifest and dashboard | area-app-model |
| #12029 | PR | Only get locations with subscription when value is a GUID | area-dashboard |
| #12028 | PR | [main] Update dependencies from microsoft/usvc-apiserver | area-codeflow |
| #12027 | PR | [main] Update dependencies from microsoft/usvc-apiserver | area-codeflow |
| #12026 | PR | Only return the single most valid developer certificate | area-app-model |
| #12025 | PR | redo extension loc | area-codeflow, area-extension |
| #12024 | PR | Update SDK to 10.0 RC2 | area-codeflow |
| #12021 | PR | Ensure adding custom certificate references to a resource is idempotent | area-app-model |
| #12020 | PR | Revise .NET version support in README.md | needs-area-label |
| #12018 | Issue | Ensure that `WithCertificateAuthorityCollection` is idempotent if called multiple times with the same certificate collection | area-app-model |
| #12017 | PR | [main] Update dependencies from microsoft/usvc-apiserver | area-codeflow |
| #12016 | Issue | Create a new configuration key for DeveloperCertificateTrust without requiring an options parameter. | area-app-model |
| #12015 | Issue | Move certificate trust environment and argument configs up the stack to use annotations | area-app-model |
| #12014 | PR | bump extension version to 0.4.0 | area-codeflow, area-extension |
| #12013 | PR | Write apphost logs to debug session console | needs-area-label |
| #12012 | PR | update package per msft policy | area-integrations |
| #12010 | PR | Add run/debug editor commands | area-extension |
| #12008 | PR | Prompt to set apphostpath on extension activation | area-cli, area-extension |
| #12007 | Issue | Prompt for apphostpath creation in a new workspace (only if apphosts exist) | area-cli, area-extension |
| #12005 | PR | Record errors in AI subscription callbacks to telemetry | area-dashboard |
| #12003 | PR | Update withdockerfile playground to use Microsoft Container Registry images |  |
| #12002 | PR | Add comprehensive unit tests for dashboard language icons (PR #11999) |  |
| #12000 | PR | Fix error when navigating to traces page with copilot sidebar open | area-dashboard |
| #11999 | PR | Add dashboard language icons for Python, JavaScript and C# | area-dashboard, area-app-model |
| #11998 | PR | Added support for enabling Application Insights for App Service | area-integrations, community-contribution |
| #11997 | PR | Fix `aspire init` cross-drive directory operation on Windows | area-cli |
| #11995 | PR | Add interaction service comboxbox filtering | area-dashboard |
| #11991 | PR | Rename AddDockerfile and WithDockerfile overloads to improve IntelliSense |  |
| #11990 | Issue | Add a Python starter template | area-templates, python, area-cli |
| #11988 | Issue | `aspire update` for single file apphost | area-cli |
| #11987 | PR | Add conditional skip to DeployAsync_ShowsEndpointOnlyForExternalEndpoints test for AzDO environments | area-engineering-systems, testing ☑️ |
| #11986 | Issue | Update templates with AppHost & web projects/files to use *.dev.localhost on .NET 10 | area-templates |
| #11985 | Issue | Azure Provisioning during run doesn't support filtering subscriptions in dialog | area-dashboard |
| #11982 | PR | Include aspire-apphost-singlefile in default templates when feature is enabled |  |
| #11979 | PR | [WIP] Fix URL rendering in console for linkability | area-cli |
| #11978 | PR | Improve Azure provisioning interaction dialog | area-app-model |
| #11977 | PR | [main] Update dependencies from dotnet/arcade | area-codeflow |
| #11976 | PR | Davidfowl/deploy ux | area-cli |
| #11975 | PR | Use new interaction input features in Azure provisioning | area-dashboard |
| #11974 | PR | Fix race condition in concurrent DeploymentState access causing intermittent AzureDeployerTests failures |  |
| #11973 | PR | Fix culture-specific decimal formatting in ResourceContainerImageBuilder |  |
| #11972 | PR | Bump tar-fs from 2.1.3 to 2.1.4 in /extension in the npm_and_yarn group across 1 directory | area-codeflow, community-contribution, javascript, dependencies |
| #11971 | PR | Fix interaction inputs dialog from extending past the dialog size | area-dashboard |
| #11965 | PR | Modify test command and clarify filter usage | area-integrations |
| #11964 | PR | Support Dockerfile generation for YARP static files in publish mode |  |
| #11963 | Issue | Support Dockerfile generation for YARP static files in publish mode | area-integrations, yarp |
| #11962 | Issue | [WebToolsE2E] There is no aspire version option when creating 10.0 Aspire Empty App. | area-templates |
| #11961 | PR | Add Dockerfile generation for Python applications in PublishAsDockerF… | area-integrations, python |
| #11960 | PR | Improve CLI compatibility with interaction prompts | area-cli |
| #11958 | PR | Fix errors when writing the manifest file with relative paths specified as output path | area-app-model |
| #11957 | PR | Don't try to run non-project types in IDE unless SupportedLaunchConfigurations property exists | area-app-model |
| #11956 | PR | Update .NET Aspire version table: change 10.x to 13.x and remove 11.x row |  |
| #11955 | Issue | Remove DeployingCallbackAnnotation in favor of PipelineStepAnnotation | area-deployment |
| #11953 | PR | Add initial support for modeling deployment pipelines in Aspire | area-deployment |
| #11951 | PR | Add noProfileSwitch to run command in DotNetCliRunner | area-cli |
| #11950 | Issue | InteractiveInput dialogue display issue | area-dashboard, needs-area-label, interaction-service |
| #11947 | PR | Add XML Documentation Standards and Apply Improvements to Aspire.Hosting.Python |  |
| #11945 | PR | Make WithUvEnvironment idempotent by checking for existing resource |  |
| #11942 | PR | Add Aspire.Hosting.Maui (.NET MAUI) integration | area-integrations |
| #11941 | PR | Disable .NET SDK's Workload Resolver in Aspire AppHost projects |  |
| #11939 | PR | Add AddDockerComposeFile to import Docker Compose files into Aspire application model |  |
| #11938 | PR | Implement Connection Properties and supporting expressions | area-app-model |
| #11937 | PR | Add `WithUvEnvironment` extension method to `Aspire.Hosting.Python` |  |
| #11936 | PR | Fix GetSupportedLaunchConfigurations to return default ["project"] instead of null | area-app-model, needs-author-action |
| #11935 | Issue | Executable resources try to run in IDE even when not explicitly allowed by the IDE protocol | area-app-model, area-tooling |
| #11934 | Issue | Add `WithPythonUvEnvironment` to Python extension builder in `Aspire.Hosting.Python` | area-integrations, python |
| #11933 | PR | Improve error reporting in InitCommand by adding OutputCollector support |  |
| #11932 | PR | Multi-target all component libraries to fix NuGet downgrade error |  |
| #11931 | PR | Add the ability to get a host address ReferenceExpression from an EndpointReference | area-app-model |
| #11930 | Issue | Aspire SDK should disable the .NET SDK's Workload Resolver | area-acquisition, perf |
| #11929 | PR | Fix bind mount names generated for Azure Container Apps to use valid Bicep identifiers |  |
| #11926 | PR | Fix spelling in project templates | area-templates, community-contribution |
| #11921 | PR | Add NuGet.config creation to solution initialization in `aspire init` |  |
| #11920 | PR | Move SelectViewModel out of Otlp namespace | area-dashboard |
| #11919 | PR | Fix not focusing choice inputs | area-dashboard |
| #11918 | PR | Default Choice inputs to first option when no placeholder is set and custom choice is disabled | area-dashboard |
| #11916 | PR | [Automated] Update AI Foundry Models | area-integrations, area-engineering-systems, Re-opened Github-Action PR |
| #11915 | PR | Clean up system level logs in resource output |  |
| #11914 | Issue | Clean up system level logs in output | area-app-model |
| #11912 | PR | Fix missing space in CLI | area-cli |
| #11909 | PR | Add editor run and debug apphost commands | area-extension |
| #11905 | PR | Revert SQL Server container image tag from 2025-latest to 2022-latest for Mac ARM compatibility |  |
| #11902 | Issue | Aspire extension continues to show `Adding Aspire hosting integration...` | area-extension |
| #11901 | PR | [WIP] Add extension methods for NodeAppResource in NodeAppHostingExtensions |  |
| #11898 | PR | ignore focus out in string prompts | needs-area-label |
| #11897 | PR | indicate that all dcp protocols are supported | needs-area-label |
| #11895 | PR | Update LogsAvailable to return true for any non-null/non-empty resource state |  |
| #11893 | PR | Fix AddPythonApp to support deferred virtual environment creation |  |
| #11892 | PR | Fix AddPythonApp parameter ambiguity using OverloadResolutionPriority |  |
| #11891 | PR | support single-file apphosts | area-extension |
| #11890 | PR | Update default Kusto emulator db creation script to be persistent | area-integrations, community-contribution |
| #11889 | PR | Add initial support for custom certificate trust | area-app-model |
| #11887 | Issue | add telemetry for CLI + extension | area-cli, area-extension |
| #11881 | PR | Introduce YarpNpmResource fluent API for Node static frontend scenarios |  |
| #11879 | PR | Change Python OTEL configuration to environment variables without requiring opentelemetry-instrument |  |
| #11878 | Issue | Change Python OTEL configuration to environment variables | area-telemetry, python |
| #11877 | PR | Support storing state in filesystem for local deploys |  |
| #11876 | PR | Queue document update | area-integrations, community-contribution |
| #11873 | PR | Add experimental restart loop for exit code 88 in RunCommand |  |
| #11872 | PR | Add system log stream source and support timezone formats without colon |  |
| #11871 | Issue | Add a new `system` log stream source | area-app-model |
| #11870 | PR | Follow-up changes to PR #11852: Preserve ContainerImageAnnotation and encapsulate dockerfile factory logic | breaking-change |
| #11869 | PR | Add MinimumTlsVersion to storage accounts and eliminate duplication with shared helper |  |
| #11867 | PR | [main] Update dependencies from microsoft/usvc-apiserver | area-codeflow |
| #11865 | Issue | Aspire Python Templates for Python Backend and Javascript Frontend | area-templates, area-acquisition, python |
| #11864 | Issue | Aspire CLI: Auto-acquire .NET SDK to Remove Prerequisite for Non-.NET Developers | area-acquisition |
| #11863 | PR | Add support for custom icons in resources beyond FluentUI icon names | area-dashboard |
| #11862 | Issue | Aspire Dashboard Standalone Docker Image 9.5 not deployed to any registry | area-dashboard |
| #11861 | PR | Update key sizes in aspire extension to 4096 bits | area-extension |
| #11860 | Issue | Change key sizes to 4096 | area-extension |
| #11859 | Issue | Add editor commands for run/debug apphost  | area-extension |
| #11858 | Issue | Support new DCP protocol updates | area-extension |
| #11856 | PR | React to interface changes | needs-area-label |
| #11854 | PR | Add updateSolutionEnabled feature flag for aspire update command |  |
| #11853 | PR | Add TFM tracking to InitContext for determining required AppHost framework | area-cli |
| #11852 | PR | Add support for dynamic Dockerfile generation via callback functions in WithDockerfile | needs-area-label |
| #11851 | Issue | Support dynamic Dockerfile generation via callback in WithDockerfile | area-app-model |
| #11849 | PR | [main] Update dependencies from dotnet/arcade | area-codeflow |
| #11848 | PR | Improve trace details performance with a very large trace | area-dashboard |
| #11846 | PR | Add runtime type validation for AddCluster API |  |
| #11845 | PR | Add GetEndpoint APIs to expose DevTunnel endpoint references |  |
| #11844 | PR | [WIP] Expose APIs to get an EndpointReference from a DevTunnelResource |  |
| #11843 | Issue | Expose APIs to get an EndpointReference from a DevTunnelResource | area-integrations |
| #11840 | PR | Add WithChildRelationship methods for parent-child relationships | area-integrations, community-contribution |
| #11839 | Issue | Add WithChildRelationship Methods for Intuitive Parent-Child Resource Relationships | area-app-model |
| #11838 | Issue | Dashboard is slow to expand and collapse span with a lot of children | area-dashboard |
| #11836 | PR | EndpointReference resolution waits for allocation to resolve values | breaking-change |
| #11835 | PR | Fix message bar display when sidebar is open | area-dashboard |
| #11834 | Issue | EndpointReference.GetValueAsync does not reliably wait for endpoint allocation | area-app-model, breaking-change |
| #11833 | Issue | Dev tunnel resource doesn't pickup change to allow anonymous access | area-integrations, devtunnels |
| #11832 | PR | Default AZURE_TOKEN_CREDENTIALS env var when running in Azure | area-integrations |
| #11831 | PR | Ensure OutputPath is created in ResourceContainerImageBuilder | area-app-model |
| #11830 | PR | Fix TryGetComputeResourceEndpoint to only show external endpoint URLs |  |
| #11829 | Issue | `TryGetComputeResourceEndpoint` should account for internal endpoints | area-deployment |
| #11828 | PR | Update IDE protocol version list | area-codeflow |
| #11827 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2808725 | area-templates |
| #11826 | PR | Add local deploy state for provisioning | area-app-model |
| #11821 | PR | Clean up Markdown duplication | area-dashboard |
| #11817 | PR | Improve `aspire init` experience with project addition to app host and service defaults referencing. | area-cli |
| #11814 | PR | Invoke ParameterProcessor as a pre-requisite for all deployment steps |  |
| #11812 | PR | Remove Microsoft.Extensions.ServiceDiscovery package source mappings from PackagingService | area-cli |
| #11811 | PR | Remove Microsoft.Extensions.ServiceDiscovery projects and use NuGet packages v9.5.0 |  |
| #11810 | PR | Make ProjectDirectory publicly settable in DistributedApplicationOptions |  |
| #11809 | PR | Skip interactive prompts in DotNetTemplateFactory when init command uses single-file AppHost |  |
| #11808 | PR | Add showAllTemplates feature flag to Aspire CLI |  |
| #11806 | PR | Dashboard AI assistant | area-dashboard |
| #11805 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2807458 | area-templates |
| #11804 | PR | Add supported launch configurations to IDE endpoint information | needs-area-label |
| #11803 | PR | Add python launch configuration entry, various python integration cleanup | area-app-model, area-extension |
| #11801 | PR | Bump the npm_and_yarn group across 3 directories with 2 updates | area-codeflow, community-contribution, javascript, dependencies |
| #11800 | PR | Fix CLI prompt labels not showing for single-input prompts |  |
| #11796 | PR | Add "just now" display for recent health checks with tooltip showing last run time | area-dashboard |
| #11791 | PR | Improve GenAI visualizer error handling | area-dashboard |
| #11786 | PR | [CI] Always generate overall test results summary | area-engineering-systems |
| #11785 | PR | Support parameter names with dashes resolved from underscore configuration |  |
| #11784 | PR | Disable tests failing on AzDO to get the pipeline green | area-engineering-systems, testing ☑️ |
| #11783 | PR | Fix CommandLineArgsCallbackContext ExecutionContext in AzureResourcePreparer and prevent WithVSCodeDebugSupport execution in publish mode |  |
| #11780 | PR | Revamp activity reporter to support multiple steps and tasks in parallel | area-deployment |
| #11778 | PR | Remove ServiceDiscovery libraries moved to dotnet/extensions |  |
| #11772 | PR | Add fallback support for parameter names with dashes to enable command-line configuration |  |
| #11768 | PR | Fix secret prompts not rendering as password fields in VS Code extension |  |
| #11767 | Issue | [VS Code Extension]: Secret prompts don't render as secret | area-extension |
| #11766 | PR | Change GenAIVisualizerDialogViewModel's PeerName and SourceName to init instead of set |  |
| #11763 | PR | Adapt OpenAI health check based on endpoint configuration |  |
| #11762 | Issue | OpenAIHealthCheck does not use custom endpoint | area-integrations |
| #11761 | PR | Give dashboard resource URLs display names | area-dashboard |
| #11759 | PR | Fix console logs race condition | area-dashboard |
| #11758 | PR | Fix GenAI visualizer when span is missing peer attribute | area-dashboard |
| #11754 | PR | Add directory path support with recursive search to `aspire add --project` and `aspire update --project` |  |
| #11753 | Issue | Invoke ParameterProcessor as a pre-requisite for all deployment steps | area-deployment |
| #11752 | PR | Update retry in Kusto emulator actions to handle any non-permanent error | area-integrations, community-contribution |
| #11751 | Issue | Remove image build from Docker Compose publisher | area-deployment |
| #11750 | Issue | Model steps in deployment process as individual steps | area-deployment |
| #11749 | PR | Add issue reference to SpecializedTestRunsheetBuilderBase.targets comment |  |
| #11748 | PR | Move all Kusto types into Aspire.Hosting.Azure namespace | area-integrations, community-contribution |
| #11746 | PR | Add session message notification type to IDE protocol | area-tooling, area-orchestrator |
| #11744 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2807045 | area-templates |
| #11743 | Issue | `aspire add` project argument should accept directory paths | area-cli |
| #11742 | Issue | GitHubModel enum cannot use implicit using while builder method can | area-app-model |
| #11738 | PR | Truncate span name with ellipsis | area-dashboard |
| #11737 | PR | Fix HTTP display summary with newer HTTP client telemetry | area-dashboard |
| #11734 | PR | Fix ParameterProcessor to use ExecutionContextOptions and skip excluded resources |  |
| #11733 | PR | Don't require gen_ai.system attribute on span events | area-dashboard |
| #11732 | PR | Disable `buildx` dependent tests on AzDO build machines | area-engineering-systems |
| #11731 | PR | [WIP] Quarantine failing Docker buildx tests on Azure DevOps |  |
| #11729 | PR | Refactor IconResolver to singleton with DI and logging |  |
| #11724 | PR | [CI] Prevent regressions on Outerloop, and Quarantined tests workflows | area-engineering-systems |
| #11723 | PR | Support DeploymentImageTagAnnotation in docker-compose publisher | area-deployment |
| #11722 | PR | [CI] Add path triggers and Copilot instructions to outerloop and quarantine test workflows | area-engineering-systems |
| #11719 | PR | Add comprehensive Copilot-assisted review guidelines |  |
| #11718 | PR | Reorder method calls for Kusto resource setup | needs-area-label |
| #11717 | Issue | Rogue DCP Errors on startup | area-orchestrator |
| #11714 | PR | Fix Resource Icon override behavior in ResourceNotificationService |  |
| #11713 | Issue | Resource Icon can't be overridden once set | area-app-model |
| #11705 | PR | [main] Update dependencies from dotnet/arcade | area-codeflow |
| #11704 | PR | Improve GenAI markdown preview | area-dashboard |
| #11703 | PR | Improve GenAI message parsing error handling | area-dashboard |
| #11700 | PR | Update Qdrant logo image to a new version | needs-area-label, community-contribution |
| #11698 | PR | Show health check timestamp for unhealthy resources in dashboard state column |  |
| #11695 | PR | Rename AppHost.cs to Program.cs in AspireWithNode.AspNetCoreApi sample | needs-area-label, community-contribution |
| #11685 | PR | [Automated] Update API Surface Area | NO-MERGE, Re-opened Github-Action PR |
| #11684 | Issue | Recognize "http.request.method" and "http.response.status_code" span attributes in dashboard | area-dashboard, area-telemetry |
| #11683 | PR | Fix Outerloop tests by adding missing Test target to eng/OuterPreBuild.proj | area-engineering-systems |
| #11682 | PR | Fix redundant backchannel connection logging in CLI debug mode |  |
| #11681 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2804151 | needs-area-label |
| #11680 | PR | Suppress Docker security warnings for playground applications | area-integrations |
| #11679 | Issue | Custom Certificate Trust API Proposal | area-app-model, area-orchestrator |
| #11678 | PR | Enable building of extensions in the internal build pipeline using property directly instead of switch | area-engineering-systems |
| #11673 | PR | Add configuration to suppress unsecured telemetry message in dashboard | area-dashboard |
| #11672 | Issue | Add configuration to suppress unsecured message in dashboard | area-dashboard |
| #11671 | PR | Added support for Aspire dashboard in App Service | area-integrations, community-contribution |
| #11670 | PR | Update Microsoft.Extensions.AI packages, use content env var |  |
| #11666 | PR | Remove memory limit from Kusto emulator | area-integrations, community-contribution |
| #11665 | PR | Merge in changes from 'release/9.5' branch | needs-area-label |
| #11661 | PR | Localized file check-in by OneLocBuild Task: Build definition ID 1309: Build ID 2802959 | area-cli |
| #11657 | PR | Refactor Devcontainer port forwarding logic to use a channel for updates and improve asynchronous processing. | needs-area-label |
| #11656 | PR | Display help text for GenAI sensitive data when no messages | area-dashboard |
| #11652 | PR | Add --non-interactive flag to dotnet watch, skip watch mode for single file apps, add debug support, and suppress browser launch |  |
| #11648 | PR | Fix console output rendering glitch for endpoints in aspire run command |  |
| #11646 | PR | Add derived Azure resource classes for accessing original compute resources |  |
| #11643 | PR | [Automated] Update Playground Manifests | area-app-model, area-engineering-systems, Re-opened Github-Action PR |
| #11642 | PR | Put the deploy command behind a feature flag |  |
| #11640 | PR | Move AzureKustoBuilderExtensions and related classes to Aspire.Hosting namespace |  |
| #11639 | PR | Move AddAzureKustoCluster extension method to Aspire.Hosting namespace |  |
| #11635 | PR | Support dynamic inputs | area-dashboard |
| #11634 | PR | Fix key of helm values section to match HelmExpression when using resources names with dashes (re-opened) | needs-area-label, community-contribution |
| #11633 | PR | Remove --watch flag from aspire run and add features.defaultWatchEnabled feature flag | breaking-change |
| #11632 | Issue | Make dotnet watch the default in aspire run when features.defaultWatchEnabled is set | area-cli |
| #11631 | PR | Fix NuGet version range display in ProjectUpdater to prevent Spectre Console crashes |  |
| #11630 | PR | Fix aspire update command failing on version ranges in PackageReference |  |
| #11629 | PR | Fix aspire update command crash with NuGet version range expressions |  |
| #11628 | PR | Fix endpoint rendering colon wrapping issue in constrained terminals | area-integrations, community-contribution |
| #11624 | PR | Fix globalPackagesFolder path to be platform-agnostic in NuGetConfigMerger |  |
| #11621 | PR | [main] Update dependencies from microsoft/usvc-apiserver | area-codeflow |
| #11620 | PR | Add --project switch to aspire update command | area-cli |
| #11619 | PR | Only run custom targets in polyglot samples if required commands are present |  |
| #11614 | Issue | Only run custom targets in polyglot samples if required commands are present | area-samples |
| #11613 | PR | note about python in linux | needs-area-label, community-contribution |
| #11611 | PR | Add a size check for the max size of the aspire CLI executable. | area-engineering-systems |
| #11610 | PR | Also sort by updated_at  in get-aspire-cli-pr script | area-engineering-systems |
| #11609 | PR | Ensure that the Tool form of the Aspire CLI is platform-independent, framework-dependent. | needs-area-label |
| #11607 | Issue | Non-standard UX for VS Code extension | area-extension |
| #11601 | PR | [draft] single file appost support | area-cli |
| #11599 | PR | Allow .NET 10 prerelease versions for single-file apphost scenarios | area-cli |
| #11594 | PR | Fix CI workflow to prevent build failures from numeric SHA values in PR version suffixes | area-engineering-systems, needs-area-label |
| #11593 | PR | Add staging channel support to Aspire CLI for dogfooding staging builds | area-cli |
| #11592 | Issue | AIFoundryModel strongly-typed model catalog fails on local, GPU-enhanced models. | area-integrations, ai |
| #11588 | Issue | Update `aspire update` to support single-file apphost | area-cli |
| #11587 | PR | Add MSBuild project to ensure all project files have unique names |  |
| #11584 | Issue | Dev tunnel port resources show as healthy even when parent tunnel resource is unhealthy | area-dashboard, devtunnels |
| #11583 | PR | Fix Yarp playground app | area-app-model |
| #11582 | PR | fix redis and rabbitmq docs | documentation, needs-area-label |
| #11580 | Issue | Dev tunnels URLs change between runs | area-integrations, devtunnels |
| #11579 | PR | Avoid run-mode specific prompts in parameter processing | area-app-model |
| #11577 | PR | Fix ai foundry model gen | area-app-model |
| #11574 | PR | Don't add reference to dev tunnel endpoint when publishing | area-app-model |
| #11573 | PR | Add proper launch profile support to the VS Code extension | area-extension |
| #11572 | Issue | Add support for *.dev.localhost host name to Aspire templates when targeting .NET 10 | area-templates |
| #11570 | PR | Fix flashing console windows when Docker processes are launched on Windows |  |
| #11569 | PR | do not print ctrl+c message if running in extension | area-codeflow |
| #11568 | PR | [Automated] Update dependencies | Re-opened Github-Action PR |
| #11567 | PR | Fix DevTunnels hanging during manifest publishing in CI workflows | area-app-model |
| #11566 | PR | Add self upgrade comand for CLI | community-contribution, area-cli |
| #11565 | PR | Add ConfigureInfrastructure sample for Kusto SKU customization to README |  |
| #11564 | PR | Fix thread safety issue in OutputCollector causing ArgumentOutOfRangeException in aspire new | area-cli |
| #11563 | PR | Switch Azure Kusto integration to use StandardE2av4 SKU by default |  |
| #11561 | PR | Add emulator support to Azure Key Vault hosting |  |
| #11560 | PR | Introduce aspire init command with VS Code extension support, enhanced user experience, robust template version management, and comprehensive solution discovery |  |
| #11559 | Issue | Overhaul aspire new and introduce aspire init command | area-templates |
| #11558 | PR | [main] Update dependencies from dotnet/arcade | area-codeflow |
| #11556 | PR | Add option to allow custom choice with choice input | area-dashboard |
| #11555 | PR | Add experimental Dockerfile builder API for programmatic container file generation with composable WithDockerfile integration |  |
| #11554 | PR | Add optional callback parameter to NuGetConfigMerger for user confirmation and preview with ProjectUpdater integration, CancellationToken support, and localized strings |  |
| #11552 | PR | Add infrastructure for Git-backed templates in Aspire CLI |  |
| #11551 | PR | Add deny list of schemes to not display as links | area-dashboard |
| #11549 | PR | Prefer older nowrap CSS rule | area-dashboard |
| #11548 | PR | Display multiple log lines in tooltip | area-dashboard |
| #11547 | PR | Dev tunnel improvements | devtunnels |
| #11545 | Issue | aspire publish for docker compose should not do container builds | breaking-change, area-deployment, docker-compose |
| #11537 | PR | Allow all URL schemes to be displayed as clickable links in dashboard |  |
| #11536 | Issue | Console output in Codespaces/devcontainers for `aspire run` has rendering glitch for endpoints | area-cli |
| #11532 | PR | Fix 9.5 option in templates & update 9.4 option to 9.4.1 | area-templates, Servicing-approved |
| #11530 | PR | Fix remove logs error | area-dashboard |
| #11529 | Issue | [VS Code Extension] Aspire:Deploy with unresolved parameters cancels the deploy when I click to another window | area-extension |
| #11528 | Issue | [VS Code Extension] Aspire:Deploy with unresolved parameters pops an alert window | area-deployment, area-cli |
| #11527 | PR | Improve log entry tooltip | area-dashboard |
| #11525 | PR | Ensure devtunnel URLs are updated when started | needs-area-label |
| #11524 | Issue | Refactor PublishingActivityReporter and steps/tasks for more concurrent processing | area-deployment |
| #11523 | PR | Update extension strings and readme | needs-area-label |
| #11521 | Issue | Add type-checks and overloads for AddCluster API | area-integrations, yarp |
| #11520 | PR | 9.5 API Review Feedback Round 1 | needs-area-label |
| #11515 | PR | Fix peer matching with container to host calls | area-dashboard |
| #11513 | PR | Upgrade App Configuration dependency | needs-area-label, community-contribution |
| #11511 | PR | Add WithVirtualEnvironment extension method for Python resources |  |
| #11505 | PR | [WIP] Cherrypick commit f43772f872f1bcb386854544a901370700963759 to release/9.5 and use this template in the PR description:  Backport of #11264 to release/9.5  ## Customer Impact  This change makes Kusto deployable  ## Testing  - Automated and manual testin... |  |
| #11504 | PR | Set up extension build and sign pipeline | area-engineering-systems |
| #11502 | PR | Add AddCSharpApp method to add C# file-based apps & projects | area-app-model |
| #11498 | Issue | Add support for adding a C# file-based app as a resource | area-app-model |
| #11496 | PR | Inventory as code | area-codeflow |
| #11493 | PR | Fix AddPackageAsync to use --version flag only for non single-file scenarios | area-cli |
| #11484 | PR | extension CI [draft] | area-engineering-systems |
| #11471 | PR | Refactor dashboard accent colors | area-dashboard |
| #11467 | PR | Remove duplicated filters on structured logs page | area-dashboard |
| #11465 | PR | Add duration column and sorting to test summary generator |  |
| #11463 | PR | Always upload logs in build-packages workflow for debugging failed builds |  |
| #11460 | PR | Add package version suffix extraction to get-aspire-cli-pr scripts | needs-area-label |
| #11459 | PR | Update Aspire branding and version references from 9.5.0 to 9.6.0 |  |
| #11454 | PR | Refactor ContainerRuntime implementations to share code via base class |  |
| #11453 | Issue | `aspire update` ignores `nuget.config` | 💥 blocking-release 💥, area-cli |
| #11446 | PR | Fix text visualizer dialog max height | area-dashboard |
| #11444 | Issue | Implement deployment state caching for `aspire deploy` | area-deployment |
| #11425 | PR | [CI] Add `azdo-tests` pipeline to run azdo tests with `/azp run` |  |
| #11412 | PR | [CI] Fix `update-dependencies` workflow | area-engineering-systems |
| #11367 | PR | Replace MSBuild interrogation with XML-based AppHost detection in ProjectLocator |  |
| #11339 | PR | integrate better into vs code apis | needs-area-label |
| #11326 | PR | Refactor Aspire CLI internal APIs from JsonDocument to JsonObject |  |
| #11315 | PR | Add build and release pipelines for vs code extension | area-engineering-systems |
| #11301 | Issue | Expose compute pointer to Aspire resource associated with compute resource | area-deployment |
| #11300 | PR | Expose WaitForText* methods in Aspire.Hosting.Testing for first-class testing support |  |
| #11296 | Issue | Clean up error handling in CLI | area-cli |
| #11290 | PR | Fix AIFoundryModel.Microsoft.Phi35MiniInstruct model name by updating generation tool |  |
| #11276 | PR | Add primitive Files resource to Aspire.Hosting |  |
| #11266 | PR | Obsolete the `IDistributedApplicationLifecycleHook` interface and extension methods and add a new eventing based replacement | area-app-model, breaking-change |
| #11264 | PR | Add provisioning support to Aspire.Hosting.Azure.Kusto |  |
| #11231 | PR | Add WithReverseProxy and PublishWithReverseProxy extension methods for Keycloak deployment to Azure Container Apps |  |
| #11203 | PR | Enhance aspire update command to update templates, handle workload conflicts gracefully, and support Central Package Management detection |  |
| #11188 | PR | Add support for multiple Unix domain socket connections to enable `aspire log` command |  |
| #11187 | PR | Fix concurrent interactive display issue in aspire CLI lazy SDK auto-installation |  |
| #11134 | PR | [WIP] Copy changes from PR #11091 (aspire-run-file) into a new coding agent PR |  |
| #11126 | PR | Remove experimental attributes from WithVolume extension methods for ProjectResource |  |
| #11123 | PR | Implement VSCode extension recommendation feature for Aspire CLI and AppHost |  |
| #11076 | PR | Add WithVolume overloads for easier ownership and permissions configuration |  |
| #11046 | Issue | Aspire fails to correctly detect ProjectResource exit. Resource state is "Unknown" in the dashboard | area-orchestrator |
| #11000 | Issue | Aspire 9.4.x dashboard not connecting as expected | area-orchestrator, ‼️regression-from-last-release |
| #10993 | Issue | Aspire CLI shows versions *and* channels side by side in version selection for aspire new and aspire add | area-cli |
| #10957 | Issue | Add built-in MCP server (preview) to dashboard with streaming HTTP and minimal tool surface | area-dashboard, ai |
| #10930 | PR | Add WithEnvFile extension methods with improved parameter handling and resource-specific directory resolution |  |
| #10856 | PR | Add workflow to automatically upgrade extension npm dependencies |  |
| #10820 | Issue | Helm output of 'aspire publish' produces invalid yaml when services have dashes in name | area-deployment, kubernetes |
| #10786 | PR | Remove unused Versions.targets and all CI: false workarounds | area-engineering-systems, needs-area-label |
| #10783 | Issue | [9.4.0] Aspire dashboard shows incorrect status and console if >1 of same Project is run | area-app-model, area-tooling, vs-code, ‼️regression-from-last-release |
| #10763 | PR | [Automated] Update API Surface Area | NO-MERGE, Re-opened Github-Action PR |
| #10761 | PR | Update OTLP chart colors with theme-aware Fluent Design color palettes |  |
| #10734 | PR | Deprecate AfterResourcesCreatedEvent due to inconsistent firing behavior |  |
| #10655 | Issue | Improve error experience when Docker image build fails during aspire publish | area-app-model |
| #10617 | Issue | Automatically install .NET SDK when using Aspire CLI | area-acquisition, area-cli |
| #10531 | PR | Fix key of helm values section to match HelmExpression when using resources names with dashes. | needs-area-label, community-contribution |
| #10429 | PR | Update Microsoft.Playwright to 1.53.0 | area-integrations |
| #10376 | Issue | AddPythonApp fails too early when there's no virtual environment | area-integrations, python |
| #10333 | Issue | Aspire.Hosting.Yarp unable to proxy to https endpoints | area-integrations, yarp |
| #10311 | PR | Implement RFC6761 reserved DNS names handling | area-service-discovery |
| #10167 | PR | Target net10.0 in Aspire.Cli | area-cli |
| #9821 | Issue | aspire publish not working with .AddNpmApp() | area-deployment, docker-compose |
| #9782 | Issue | Provide ability to specify the azure tenant id for local provisioning | area-integrations, azure |
| #9681 | PR | Leverage VerifyFile | area-integrations, community-contribution |
| #9663 | PR | leverage Verify.SystemJson to support verify for JsonNode | area-integrations, community-contribution |
| #9627 | PR | Apply Verify conventions | area-integrations, community-contribution |
| #9606 | Issue | Add dashboard support to AzureAppServiceEnvironmentResource | azure, area-deployment, azure-app-service |
| #9591 | PR | [WIP] Show time alongside health check status |  |
| #8769 | Issue | aspire publish shows 0% in non interactive scenarios for progress | area-cli |
| #8396 | PR | support ClaimActions configure for dashboard | area-dashboard, community-contribution |
| #8350 | Issue | Make Aspire better for client-side JavaScript developers. | area-integrations, javascript |
| #8244 | Issue | Codespaces Aspire process start time mismatch, pid might have been reused | area-dashboard, area-orchestrator |
| #6632 | Issue | Python hosting integration doesn't support command other than `python` | area-integrations |
| #6547 | Issue | Container to Host networking does not work consistently | area-app-model, area-orchestrator |
| #6244 | Issue | Add first class support for running python apps with specified modules | area-integrations, python |
| #6243 | Issue | Fix args parameter in AddPythonApp | area-integrations, python |
| #6237 | Issue | AddPythonApp issues | area-integrations, python |
| #5453 | Issue | Aspire Assistance with HTTPS | area-dashboard, area-telemetry |
| #4872 | Issue | Azure provisioning using visual studio account not configured account | area-integrations, azure |
| #4131 | Issue | Container telemetry not shown in AppHost dashboard | area-telemetry, rancher |
| #3544 | Issue | Azure Provisioning fails on secondary tenant | area-integrations, azure, area-deployment |
| #3324 | Issue | Node.js app resources added with AddNpmApp/AddNodeApp fail to fetch content from ASP.NET Core apps over HTTPS | area-app-model, security 🔐 |
| #3117 | Issue | Usernames and passwords in connection strings aren't escaped | area-app-model, area-azd |
| #2111 | Issue | Support multiple connection string formats | area-app-model |
