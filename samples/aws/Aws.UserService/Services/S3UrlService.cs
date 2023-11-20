// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3;
using Aws.UserService.Contracts;
using LocalStack.Client.Options;
using Microsoft.Extensions.Options;

namespace Aws.UserService.Services;

public class S3UrlService : IS3UrlService
{
    private readonly LocalStackOptions _localStackOptions;

    public S3UrlService(IOptions<LocalStackOptions> localStackOptions)
    {
        _localStackOptions = localStackOptions.Value;
    }

    public string GetS3Url(IAmazonS3 amazonS3, string bucket, string key)
    {
        if (_localStackOptions.UseLocalStack)
        {
            return $"http://localhost:4566/{bucket}/{key}";
        }

        var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

        if (string.IsNullOrWhiteSpace(awsRegion))
        {
            var amazonS3Config = (AmazonS3Config)amazonS3.Config;
            awsRegion = amazonS3Config.RegionEndpoint.SystemName ?? "us-east-1";
        }

        return $"https://{bucket}.s3.{awsRegion}.amazonaws.com/{key}";
    }
}
