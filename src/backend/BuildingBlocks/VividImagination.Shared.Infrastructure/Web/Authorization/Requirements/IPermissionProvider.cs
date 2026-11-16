namespace VividImagination.Shared.Infrastructure.Web.Authorization.Requirements;

public interface IPermissionProvider
{
    string  Scope { get; }
    /// <summary>
    /// Get permissions
    /// </summary>
    /// <returns>Permissions</returns>
    IEnumerable<PermissionRequirement> GetPermissions();

    /// <summary>
    /// Get default Role permissions
    /// </summary>
    /// <returns>Default permissions</returns>
    IEnumerable<DefaultRolePermission> GetDefaultRolePermissions();
    
    
}