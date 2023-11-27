using Aws.UserService.Models;

namespace Aws.UserService.Contracts;

public interface IProfileService
{
    Task<Profile> AddProfileAsync(Profile profile);
}