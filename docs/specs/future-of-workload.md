# Design proposal for the .NET Aspire workload going forward

.NET Aspire currently ships as a .NET SDK Workload. This workload currently includes the following components:

- Aspire.Hosting.SDK (MSBuild SDK): This SDK contains some props and targets with the required logic to properly handle `ProjectReferences` in the .NET Aspire AppHost projects. This component cannot be changed into a regular NuGet package, as the logic it carries needs to be executed before the initial restore.
- Aspire.ProjectTemplates (.NET Template Pack): This is a set of project templates that allow users to create new .NET Aspire projects using `dotnet new` commands and Visual Studio "New Project" dialog.
- Aspire.Hosting.Orchestration (MSBuild SDK): This SDK contains two main things: the Developer Control Plane executable (DCP) as well as a targets file which defines properties for the AppHost to be able to find the DCP executable and run it.
- Aspire.Dashboard.SDK (MSBuild SDK): Similar to the Orchestration SDK, this one provides the .NET Aspire Dashboard application (a framework-dependent app that depends on .NET 8 runtime) and the targets file that defines properties for the AppHost to be able to find the Dashboard executable and launch it.
- Aspire.Hosting (Regular NuGet Library): This package isn't really required to be in the workload, as this is a PackageReference that all AppHost projects will have transitively via their reference to Aspire.Hosting.AppHost. The advantage that we get currently from having it in the workload is that this would install it into the global NuGet cache when the workload is installed, so that the AppHost projects can find it without needing to restore it from the NuGet feed. This is helpful especially for offline scenarios. The package itself provides the Aspire.Hosting library which contains all of the main logic in Aspire for building and running .NET Aspire applications.
  
## Problem statement

The current design of the workload has led to some usability issues, and after a lot of deep analysis, we finally understand what is the root cause of these issues. SDK Workloads are meant to be used for tooling components, which lifetime should be tied to the lifetime of the SDK itself. When we were originally designing the workload, we believed that both DCP and Dashboard were tooling components, and that they should be installed as part of the workload. However, we have come to realize that this is not the case. DCP and Dashboard are tightly coupled to the version of Aspire.Hosting package that the AppHost project references, and therefore are part of the AppHost itself, as opposed to tooling that supports it. This is the root cause of the issues that we have seen with the workload, such as the need to ensure that the version of the workload installed matches the project reference of Aspire.Hosting, which is also a problem when trying to work with multiple Aspire projects that reference different versions of Aspire.Hosting. SDK typically makes the assumption that customers want to always be in the latest version of all of the workloads, which is likely the right thing to do for tooling components; however, this is a problem for .NET Aspire given the tight coupling between the AppHost and the DCP/Dashboard.

## Proposed solution

Given the above, we propose to move the DCP and Dashboard components out of the workload, and into NuGet packages that are dynamically referenced by the Aspire.Hosting.SDK. This will allow the AppHost project to reference the version of Aspire.Hosting that it wants, and then the DCP and Dashboard will be referenced automatically by the Aspire SDK as dependencies of that version of Aspire.Hosting. This will allow the AppHost project to have full control over the version of the DCP and Dashboard that it wants to use, and will also allow customers to have multiple AppHost projects that reference different versions of Aspire.Hosting without any issues. This will also allow us to update the DCP and Dashboard independently of the workload, which will allow us to ship updates to these components more frequently and with less risk, with a far less complex compatibility matrix.

Also, there is no real need of including the Aspire.Hosting package as part of the workload, as there is no guarantee that this is the version that the AppHost project will want to use, therefore we would also remove this package from the workload. In the end, the workload will only contain the Aspire.Hosting.SDK and Aspire.ProjectTemplates components, which are the ones that can be truly considered as tooling components, and in both cases you would want to always be in the latest version. These two are also not tightly coupled to the version of the Aspire.Hosting package that the AppHost project references, so they can be updated independently of the AppHost.

### Pros

This proposal has many benefits which will solve the issues we currently have with the workload:

- The AppHost project will have full control over the version of the DCP and Dashboard that it wants to use.
- Customers can have multiple AppHost projects that reference different versions of Aspire.Hosting without any issues.
- We can update the DCP and Dashboard independently of the workload, which will allow us to ship updates to these components more frequently and with less risk.
- Customers can stay in the latest version of the workload without worrying about compatibility issues with the AppHost project.
- Because Aspire.Hosting.SDK targets get imported before the initial restore, we can still have the same experience as before, where the AppHost project will end up restoring only the DCP and Dashboard packages for the platform that is being targeted, as opposed to needing to restore them for all platforms.

### Cons

- Workloads have a way of keeping a reference count on workload packs, so it is able to clean them up when workloads don't reference it any more (for example, if you update your workload, then the old DCP package will be cleaned up automatically). This doesn't happen with regular NuGet packages, where customers are responsible for cleaning up their NuGet package caches themselves.