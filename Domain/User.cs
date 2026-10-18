namespace Logistics.Api.Domain;

public sealed class User
{
    private User() {}
    
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static User Create(string email,UserRole role, DateTimeOffset createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = NormalizeEmail(email),
        Role = role,
        CreatedAt = createdAt
    };
    
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}

public enum UserRole
    {
        Driver,
        Dispatcher,
        Admin
    }
