# Azure Kubernetes Service hosting integration

Use this integration to model, configure, and orchestrate an Azure Kubernetes Service (AKS) environment in an Aspire solution.

## Getting started

### Prerequisites

- An Azure subscription - [create one for free](https://azure.microsoft.com/free/)
- [Helm](https://helm.sh/docs/intro/install/) **v4.2.0 or later** on your `PATH`.

Aspire shells out to `helm upgrade --install` to deploy the generated chart and any `AddHelmChart(...)` releases, and validates the Helm version up front so missing or older installs produce a clear actionable error instead of cryptic flag failures.

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Azure.Kubernetes` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Azure.Kubernetes
```

## Usage example

In the AppHost, add an AKS environment and deploy services to it:

**C#**

```csharp
var aks = builder.AddAzureKubernetesEnvironment("aks");

var myService = builder.AddProject<Projects.MyService>()
    .WithComputeEnvironment(aks);
```

**TypeScript**

```typescript
const aks = await builder.addAzureKubernetesEnvironment("aks");

const myService = await builder.addNodeApp("myService", "../my-service", "server.js")
    .withComputeEnvironment(aks);
```

### Existing container registries

Use an existing Azure Container Registry, including one in another resource group:

**C#**

```csharp
var registry = builder.AddAzureContainerRegistry("registry")
    .PublishAsExisting("myregistry", "registry-resource-group");

var aks = builder.AddAzureKubernetesEnvironment("aks")
    .WithContainerRegistry(registry);
```

**TypeScript**

```typescript
const registry = await builder.addAzureContainerRegistry("registry");
await registry.publishAsExisting("myregistry", { resourceGroup: "registry-resource-group" });

const aks = await builder.addAzureKubernetesEnvironment("aks");
await aks.withContainerRegistry(registry);
```

Aspire grants the AKS kubelet identity `AcrPull` at the registry's scope. The deployment identity must have permission to create role assignments there, as well as push images to the registry.

`aspire destroy` preserves the existing registry only when it is outside the application's resource group. It deletes the entire application resource group, including any existing resources in that group, even those configured with `PublishAsExisting`. A registry in that group and its images are deleted.

`AcrPull` permits pulling images from every repository in the registry, including unrelated images in a shared registry. This is the same permission used for an automatically created registry. Workloads can potentially obtain kubelet identity tokens through the node's Instance Metadata Service (IMDS); enabling Workload Identity alone does not prevent this. Enforce workload isolation and review the [AKS IMDS restriction guidance](https://learn.microsoft.com/azure/aks/imds-restriction), including its preview status and host-network and privileged-workload limitations.

`aspire destroy` does not remove role assignments on a registry outside the application's resource group. Remove the kubelet's role assignment separately when it is no longer needed. Deleting its managed identity does not delete these external assignments.

### Cluster identities and permissions

Aspire provisions separate user-assigned managed identities for the control plane and kubelets. Both inherit the cluster's resource-group and subscription scope, including scopes set through provisioning customization. Neither is a workload identity:

| Identity | Permissions |
| --- | --- |
| Kubelet | `AcrPull` on the selected registry. |
| Control plane | `Managed Identity Operator` on the kubelet identity, and `Network Contributor` on virtual networks selected through environment or node-pool `WithSubnet` calls. |

These role assignments are provisioned before the cluster. AKS grants the control-plane identity its normal infrastructure permissions in the AKS-managed node resource group; Aspire does not grant Contributor on the application resource group or subscription. If provisioning customization references other external resources, such as route tables, network security groups, or private DNS zones, configure the corresponding control-plane permissions separately. See [AKS managed identity role assignments](https://learn.microsoft.com/azure/aks/managed-identity-overview#control-plane-managed-identity-role-assignments).

The registry must use RBAC Registry Permissions mode for `AcrPull` to authorize image pulls. Registries using RBAC Registry + ABAC Repository Permissions mode require different roles and are not configured automatically by this integration.

#### Updating existing clusters

Clusters deployed with earlier versions used a system-assigned control-plane identity and an AKS-created kubelet identity. Redeploying with this version changes both identities. Changing the kubelet identity can trigger node-pool upgrades, which drain and reimage nodes and can disrupt workloads. Schedule the migration accordingly and follow the [pre-created kubelet identity guidance](https://learn.microsoft.com/azure/aks/pre-created-kubelet-managed-identity).

Custom role assignments on the old identities are not transferred automatically. Grant any additional permissions to the new identities before migration, and retain the old permissions until the control plane has switched identities and node pools have finished updating. The control plane can continue using its previous identity for several hours while cached tokens expire. Remove obsolete external assignments afterward.

### Cluster tier

AKS clusters use the **Standard** control-plane tier by default, providing an uptime SLA for production workloads. Standard includes a cluster-management charge in addition to the underlying Azure resource costs. See [AKS pricing tiers](https://learn.microsoft.com/azure/aks/free-standard-pricing-tiers) for current details.

Applying updated infrastructure to an existing Aspire-managed cluster changes its tier to Standard unless explicitly overridden. Upgrading the integration package alone does not change a deployed cluster.

To use the Free tier for development or testing, customize the infrastructure in a C# AppHost:

```csharp
using Azure.Provisioning.ContainerService;

var aks = builder.AddAzureKubernetesEnvironment("aks")
    .ConfigureInfrastructure(infrastructure =>
    {
        var cluster = infrastructure.GetProvisionableResources()
            .OfType<ContainerServiceManagedCluster>()
            .Single();

        cluster.Sku.Tier = ManagedClusterSkuTier.Free;
    });
```

The control-plane tier does not change node VM sizes or provide an application availability guarantee.

### Persistent volumes

Add a persistent volume to the AKS environment and mount it into a workload:

**C#**

```csharp
var data = aks.AddPersistentVolume("data")
    .WithCapacity("20Gi");

myService.WithPersistentVolume(data, "/data", env: "DATA_PATH");
```

**TypeScript**

```typescript
const data = await aks.addPersistentVolume("data");
await data.withCapacity("20Gi");

await myService.withKubernetesPersistentVolumeMount(data, "/data", { env: "DATA_PATH" });
```

When a project or executable runs locally, `DATA_PATH` points to a persistent directory in the AppHost's Aspire store. That store is normally under the AppHost intermediate-output directory, so cleaning build outputs can remove the local data. Local containers use a worktree-scoped container volume instead when the mount names an environment variable, and one persistent-volume resource cannot be shared between local containers and local projects or executables.

In AKS, `DATA_PATH` contains `/data`, the mounted volume path. When no storage class is specified, the generated claim uses the cluster's default storage class. A standard AKS cluster dynamically provisions an Azure managed disk. To request Premium SSD storage explicitly, call `WithStorageClass("managed-csi-premium")` in C# or `withStorageClass("managed-csi-premium")` in TypeScript.

## Additional documentation

* https://aspire.dev/integrations/gallery/
* https://aspire.dev/integrations/cloud/azure/aks/
* https://learn.microsoft.com/azure/aks/

## Feedback & contributing

https://github.com/microsoft/aspire
