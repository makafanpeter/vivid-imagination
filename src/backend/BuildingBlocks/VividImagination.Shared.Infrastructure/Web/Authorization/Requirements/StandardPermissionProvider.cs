using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

public class StandardPermissionProvider(IEnumerable<IPermissionProvider> providers)
{
    public IEnumerable<IPermissionProvider> Providers { get; } = providers;

    public IEnumerable<PermissionRequirement> GetPermissions() => 
        Providers.SelectMany(p => p.GetPermissions()).Distinct();

    public static IEnumerable<DefaultRolePermission> GetDefaultRoles()
    {
        return [
            new DefaultRolePermission()
            {
                RoleType = RoleType.SystemAdmin,
                UserRoleSystemName = SystemRoleNames.SystemAdministrator,
            },
            new DefaultRolePermission()
            {
                RoleType = RoleType.User,
                UserRoleSystemName = SystemRoleNames.User,
            }
        ];
    }
}