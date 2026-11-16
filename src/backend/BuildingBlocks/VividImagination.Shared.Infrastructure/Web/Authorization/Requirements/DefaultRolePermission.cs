using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

public class DefaultRolePermission
{
    /// <summary>
    /// Gets or sets the user role system name
    /// </summary>
    public string UserRoleSystemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permissions
    /// </summary>
    public IEnumerable<PermissionRequirement> PermissionRecords { get; set; } = new List<PermissionRequirement>();

    /// <summary>
    ///  Gets or sets the role type
    /// </summary>
    public RoleType RoleType { get; set; }

    

}