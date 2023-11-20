// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Aws.UserService.Contracts;
using Aws.UserService.Models;

namespace Aws.UserService.Services;

public class ProfileService(
    IS3UrlService s3UrlService,
    IAmazonS3 amazonS3,
    IAmazonSimpleNotificationService amazonSns,
    ILogger<ProfileService> logger,
    IConfiguration configuration) : IProfileService
{
    private readonly string _bucketName = configuration.GetConnectionString("ProfilePicturesBucket") ??
                                         throw new ArgumentNullException("ConnectionStrings__ProfilePicturesBucket");

    private readonly string _topicArn = configuration.GetConnectionString("ProfilesTopic") ??
                                       throw new ArgumentNullException("ConnectionStrings__ProfilesTopic");

    public async Task<Profile> AddProfileAsync(Profile profile)
    {
        logger.LogInformation("AddProfile called: BucketName: {BucketName}, TopicArn: {TopicArn}", _bucketName, _topicArn);

        try
        {
            var bytes = Convert.FromBase64String(Base64Images.Image1!);

            await using var ms = new MemoryStream(bytes);
            var fileTransferUtility = new TransferUtility(amazonS3);
            await fileTransferUtility.UploadAsync(ms, _bucketName, profile.ImageName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error uploading profile pic to S3");
            throw;
        }

        var id = Guid.NewGuid().ToString();
        var s3Url = s3UrlService.GetS3Url(amazonS3, _bucketName, profile.ImageName!);
        var createdAt = DateTime.UtcNow;

        logger.LogInformation("Profile pic uploaded to S3. Id: {Id}, S3Url: {S3Url}, CreatedAt: {CreatedAt}", id, s3Url, createdAt);

        var createdProfile = new Profile()
        {
            Id = id,
            Name = profile.Name,
            Email = profile.Email,
            ImageName = profile.ImageName,
            ImageUrl = s3Url,
            CreatedAt = createdAt,
        };

        var publishRequest = new PublishRequest()
        {
            TopicArn = _topicArn,
            Message = JsonSerializer.Serialize(createdProfile),
        };

        var publishResponse = await amazonSns.PublishAsync(publishRequest);

        logger.LogInformation("Published to SNS. Id: {Id}, HttpStatusCode: {HttpStatusCode}", id, publishResponse.HttpStatusCode);

        if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Error publishing to SNS. Id: {Id}", id);
            throw new InvalidOperationException("Error publishing to SNS");
        }

        return profile;
    }
}
