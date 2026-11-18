using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

public static class Operations
{
    public static readonly OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = nameof(Create) };
    public static readonly OperationAuthorizationRequirement Read = new OperationAuthorizationRequirement { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Update = new OperationAuthorizationRequirement { Name = nameof(Update) };
    public static readonly OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = nameof(Delete) };
    public static readonly OperationAuthorizationRequirement List = new OperationAuthorizationRequirement { Name = nameof(List) };
    public static readonly OperationAuthorizationRequirement Verify = new OperationAuthorizationRequirement { Name = nameof(Verify) };
}