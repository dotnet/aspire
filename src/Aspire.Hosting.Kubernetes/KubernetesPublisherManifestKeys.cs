// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes;

internal static class KubernetesPublisherManifestKeys
{
    internal const string ApiVersion = "apiVersion";
    internal const string Kind = "kind";
    internal const string Metadata = "metadata";
    internal const string Name = "name";
    internal const string Labels = "labels";
    internal const string App = "app";
    internal const string Spec = "spec";
    internal const string Replicas = "replicas";
    internal const string Selector = "selector";
    internal const string Matchlabels = "matchLabels";
    internal const string Template = "template";
    internal const string Containers = "containers";
    internal const string Image = "image";
    internal const string Images = "images";
    internal const string Repository = "repository";
    internal const string Tag = "tag";
    internal const string PullPolicy = "pullPolicy";
    internal const string ImagePullPolicy = "imagePullPolicy";
    internal const string ContainerPort = "containerPort";
    internal const string Port = "port";
    internal const string Ports = "ports";
    internal const string TargetPort = "targetPort";
    internal const string Protocol = "protocol";
    internal const string Type = "type";
    internal const string Version = "version";
    internal const string AppVersion = "appVersion";
    internal const string Description = "description";
    internal const string StringData = "stringData";
    internal const string Data = "data";
    internal const string Secrets = "secrets";
    internal const string Resources = "resources";
}
