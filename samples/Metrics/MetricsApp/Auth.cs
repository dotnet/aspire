using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MetricsApp;

public static class Auth
{
    public static async Task InitializeTestUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MyUser>>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<MyUser>>();

        var user = new MyUser();
        await userStore.SetUserNameAsync(user, "test@contoso.com", CancellationToken.None);

        var result = await userManager.CreateAsync(user, "Password1!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Error creating test user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

public sealed class MyUser : IdentityUser { }

public sealed class AppDbContext : IdentityDbContext<MyUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
