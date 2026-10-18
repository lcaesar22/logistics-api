using Logistics.Api.Domain;

namespace Logistics.Api.Common.Auth;

public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Dispatcher = nameof(UserRole.Dispatcher);
    public const string Driver = nameof(UserRole.Driver);
}

public static class Policies
{
    public const string ReadShipments = "shipments:read";
    public const string WriteShipments = "shipments:write";
    public const string DeleteShipments = "shipments:delete";
    public const string ManageUsers = "users:manage";
}