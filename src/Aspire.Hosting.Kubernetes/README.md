# Kubernetes hosting integration

Use this integration to model, configure, and deploy Aspire compute resources to Kubernetes.

## Getting started

### Prerequisites

- [Helm](https://helm.sh/docs/intro/install/) **v4.2.0 or later** on your `PATH`.

Aspire shells out to `helm upgrade --install` to deploy the generated chart and validates the Helm version up front, so missing or older installs produce a clear actionable error instead of cryptic flag failures.

### Add the integration

From your AppHost directory, add the `Aspire.Hosting.Kubernetes` integration with the Aspire CLI:

```bash
aspire add Aspire.Hosting.Kubernetes
```

## Usage example

In the AppHost, add the environment:

**C#**

```csharp
builder.AddKubernetesEnvironment("k8s");
```

**TypeScript**

```typescript
await builder.addKubernetesEnvironment("k8s");
```

The dashboard container included in the published Helm chart opts out of dashboard usage and error telemetry by default (`ASPIRE_DASHBOARD_TELEMETRY_OPTOUT=true`). This does not affect the application's OpenTelemetry data sent to the dashboard.

### Volumes

Use a target-neutral volume when the Kubernetes environment's default storage policy is sufficient:

**C#**

```csharp
builder.AddProject<Projects.Api>("api")
    .WithVolume("data", "/data", env: "DATA_PATH");
```

**TypeScript**

```typescript
const api = await builder.addNodeApp("api", "../api", "server.js");
await api.withVolume("/data", "data", "DATA_PATH");
```

Projects and executables receive a workload-scoped Aspire store directory in run mode. The directory is reused across AppHost runs regardless of whether the process has a session or persistent lifetime. When published, the volume uses the Kubernetes environment's `DefaultStorageType`, which is `emptyDir` — storage lives for the lifetime of the pod and is lost when the pod restarts or is rescheduled. `DATA_PATH` contains `/data`. For storage that outlives the pod, use a [persistent volume](#persistent-volumes) instead.

### Persistent volumes

Add a persistent volume and expose its effective path through an environment variable:

**C#**

```csharp
var k8s = builder.AddKubernetesEnvironment("k8s");
var data = k8s.AddPersistentVolume("data")
    .WithCapacity("20Gi");

builder.AddProject<Projects.Api>("api")
    .WithPersistentVolume(data, "/data", env: "DATA_PATH");
```

**TypeScript**

```typescript
const k8s = await builder.addKubernetesEnvironment("k8s");
const data = await k8s.addPersistentVolume("data");
await data.withCapacity("20Gi");

const api = await builder.addNodeApp("api", "../api", "server.js");
await api.withKubernetesPersistentVolumeMount(data, "/data", { env: "DATA_PATH" });
```

When a project or executable runs locally, `DATA_PATH` points to a persistent directory in the AppHost's Aspire store. That store is normally under the AppHost intermediate-output directory, so cleaning build outputs can remove the local data. Local containers use a worktree-scoped container volume instead, provided the mount names an environment variable. Mounts that do not name one keep the persistent volume's own name for the local container volume, so data written by an earlier version of the AppHost stays attached. A single persistent-volume resource cannot be shared between local containers and local projects or executables because those execution types cannot use one backing store reliably.

When published or deployed, `DATA_PATH` contains `/data`. Applications can therefore use the same environment variable in both environments. The `isReadOnly` mount option is enforced after deployment, but Aspire cannot make a directory read-only for a process running directly on the host.

```shell
aspire publish -o k8s-artifacts
```

### Persistent volume claim customization

Use `WithConfiguration` / `withConfiguration` to customize the generated claim after the environment defaults and fluent volume settings have been applied. Multiple callbacks run in registration order and see changes made by earlier callbacks.

**C#**

```csharp
var data = k8s.AddPersistentVolume("data")
    .WithConfiguration(claim =>
    {
        claim.Metadata.Labels["example.com/retention"] = "retain";
        claim.Spec.VolumeMode = "Filesystem";
        claim.Spec.StorageClassName = "";
    });
```

**TypeScript**

```typescript
const data = await k8s.addPersistentVolume("data");
await data.withConfiguration(async (claim) => {
    const metadata = await claim.metadata.get();
    await metadata.labels.set("example.com/retention", "retain");

    const spec = await claim.spec.get();
    await spec.volumeMode.set("Filesystem");
    await spec.storageClassName.set("");
});
```

TypeScript callbacks receive live remote objects: await property and collection operations rather than assigning JavaScript properties. The supported claim surface includes labels, annotations, storage class, volume name, volume mode, access modes, and resource requests. For example, use `await metadata.annotations()`, `await spec.accessModes()`, and `await (await spec.resources.get()).requests()` to access the corresponding mutable collections.

Setting `storageClassName` to `""` requests a classless volume; setting it to `null` omits the field so Kubernetes can assign its default StorageClass. The fluent equivalents are `withStorageClass("")` and `withoutStorageClass()`. Use the existing parameterized fluent methods when storage class, capacity, or annotations should be resolved through Helm parameters; callbacks see the generated values.

Other claim fields, including selectors, data sources, volume attributes classes, resource limits, and claim name/namespace customization, remain C#-only. Setting `volumeMode` to `"Block"` does not create the workload's required `volumeDevices` bindings; the persistent-volume mount APIs generate filesystem mounts.

## Additional documentation

https://aspire.dev/integrations/gallery/
https://aspire.dev/integrations/compute/kubernetes/

## Feedback & contributing

https://github.com/microsoft/aspire
