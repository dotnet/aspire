// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.AWS.Utils;
using Aspire.Hosting.Lifecycle;
using IResource = Aspire.Hosting.ApplicationModel.IResource;

namespace Aspire.Hosting.AWS;

/// <summary>
/// The CDK Lifecycle Hook makes sure that the CDK resources are synthesized before publishing
/// </summary>
/// <param name="executionContext"></param>
internal sealed class CDKLifecycleHook(DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var parentChildLookup = appModel.Resources.OfType<IResourceWithParent>()
            .Select(x => (Child: x, Root: SelectParentCDKResource(x.Parent)))
            .Where(x => x.Root is not null)
            .ToLookup(x => x.Root, x => x.Child);

        var cdkResources = appModel.Resources.OfType<ICDKResource>();
        foreach (var cdkResource in cdkResources)
        {
            // Apply construct modifier annotations
            var constructResources = parentChildLookup[cdkResource].OfType<IResourceWithConstruct>();
            foreach (var constructResource in constructResources)
            {
                // Find Construct Modifier Annotations
                if (!constructResource.TryGetAnnotationsOfType<IConstructModifierAnnotation>(out var modifiers))
                {
                    continue;
                }

                // Modify stack
                foreach (var modifier in modifiers)
                {
                    modifier.ChangeConstruct(constructResource.Construct);
                }
            }

            var cloudAssembly = cdkResource.App.Synth();

            var outputDirectory = executionContext.IsPublishMode ? Path.Combine(Environment.CurrentDirectory, "cdk.out") : cloudAssembly.Directory;
            if (executionContext.IsPublishMode)
            {
                DirectoryCopy.CopyDirectory(cloudAssembly.Directory, outputDirectory, true);
            }

            var stackResources = parentChildLookup[cdkResource].OfType<IStackResource>().Concat([cdkResource]);
            foreach (var stackResource in stackResources)
            {
                var stack = cloudAssembly.Stacks.FirstOrDefault(stack => stack.StackName == stackResource.StackName)
                            ?? throw new InvalidOperationException($"Stack '{stackResource.StackName}' not found in synthesized cloud assembly.");

                stackResource.Annotations.Add(new CloudFormationTemplatePathAnnotation(Path.Combine(outputDirectory, stack.TemplateFile)));
                stackResource.Annotations.Add(new StackArtifactResourceAnnotation(stack));
            }
        }

        return Task.CompletedTask;

        static ICDKResource? SelectParentCDKResource(IResource resource) => resource switch
        {
            ICDKResource ar => ar,
            IResourceWithParent rp => SelectParentCDKResource(rp.Parent),
            _ => null
        };
    }
}
