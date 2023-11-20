using Amazon.S3;

namespace Aws.UserService.Contracts;

public interface IS3UrlService
{
    string GetS3Url(IAmazonS3 amazonS3, string bucket, string key);
}