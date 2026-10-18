using Logistics.Api.Domain;
using Logistics.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Common.Auth;

public static class AdminBootstrap
{
    public static async Task BootstrapAdminAsync(this WebApplication app)
    {
        var email = app.Configuration["Bootstrap:AdminEmail"];
        var password = app.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var normalised = User.NormalizeEmail(email);

        if (await db.Users.AnyAsync(u => u.Email == normalised)) return;

        var admin = User.Create(normalised, UserRole.Admin, clock.GetUtcNow());
        admin.SetPasswordHash(hasher.HashPassword(admin, password));

        db.Users.Add(admin);
        await db.SaveChangesAsync();
        
        app.Logger.LogInformation("BootstrapAdmin Successfully Created Admin User");
    }
}