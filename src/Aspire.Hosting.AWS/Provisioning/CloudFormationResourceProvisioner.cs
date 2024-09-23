// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.Provisioning;

internal abstract partial class CloudFormationResourceProvisioner<T>(ResourceLoggerService loggerService, ResourceNotificationService notificationService) : AWSResourceProvisioner<T>
    where T : ICloudFormationResource
{
    protected ResourceLoggerService LoggerService => loggerService;

    protected ResourceNotificationService NotificationService => notificationService;

    protected async Task PublishCloudFormationUpdatePropertiesAsync(T resource, ImmutableArray<ResourcePropertySnapshot>? properties = null, ImmutableArray<UrlSnapshot>? urls = default)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<ResourcePropertySnapshot>();
        }

        await NotificationService.PublishUpdateAsync(resource, state => state with
        {
            Properties = state.Properties.AddRange(properties),
            Urls = urls ?? []
        }).ConfigureAwait(false);
    }

    protected static ImmutableArray<ResourcePropertySnapshot> ConvertOutputToProperties(Stack stack, string? templateFile = null)
    {
        var list = ImmutableArray.CreateBuilder<ResourcePropertySnapshot>();

        list.Add(new ResourcePropertySnapshot(CustomResourceKnownProperties.Source, stack.StackId));

        if (!string.IsNullOrEmpty(templateFile))
        {
            list.Add(new("aws.cloudformation.template", templateFile));
        }

        foreach (var output in stack.Outputs)
        {
            list.Add(new ResourcePropertySnapshot("aws.cloudformation.output." + output.OutputKey, output.OutputValue));
        }

        return list.ToImmutableArray();
    }

    [GeneratedRegex("^(us|eu|ap|sa|ca|me|af|il)-\\w+-\\d+$", RegexOptions.Singleline)]
    private static partial Regex AwsRegionRegex();

    internal static ImmutableArray<UrlSnapshot>? MapCloudFormationStackUrl(IAmazonCloudFormation client, string stackId)
    {
        try
        {
            var endpointUrl = client.DetermineServiceOperationEndpoint(new DescribeStacksRequest { StackName = stackId })?.URL;
            if (endpointUrl == null || !endpointUrl.Contains(".amazonaws.", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!Arn.TryParse(stackId, out var arn) || !AwsRegionRegex().IsMatch(arn.Region))
            {
                return null;
            }

            var url = $"https://console.aws.amazon.com/cloudformation/home?region={Uri.EscapeDataString(arn.Region)}#/stacks/resources?stackId={Uri.EscapeDataString(stackId)}";

            return
            [
                new UrlSnapshot("aws-console", url, IsInternal: false)
            ];
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected static IAmazonCloudFormation GetCloudFormationClient(ICloudFormationResource resource)
    {
        if (resource.CloudFormationClient != null)
        {
            return resource.CloudFormationClient;
        }

        try
        {
            AmazonCloudFormationClient client;
            if (resource.AWSSDKConfig != null)
            {
                var config = resource.AWSSDKConfig.CreateServiceConfig<AmazonCloudFormationConfig>();

                var awsCredentials = FallbackCredentialsFactory.GetCredentials(config);
                client = new AmazonCloudFormationClient(awsCredentials, config);
            }
            else
            {
                client = new AmazonCloudFormationClient();
            }

            client.BeforeRequestEvent += SdkUtilities.ConfigureUserAgentString;

            return client;
        }
        catch (Exception e)
        {
            throw new AWSProvisioningException("Failed to construct AWS CloudFormation service client to provision AWS resources.", e);
        }
    }
}
