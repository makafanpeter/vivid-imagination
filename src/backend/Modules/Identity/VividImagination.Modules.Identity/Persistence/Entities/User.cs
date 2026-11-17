using Microsoft.AspNetCore.Identity;
using VividImagination.Modules.Identity.Contracts;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Identity.Persistence.Entities;

public class User: IdentityUser<long>, IHasKey<long>, IAuditable
{
    public SafeBrowsingMode SafeBrowsingMode { get; set; } = SafeBrowsingMode.BlurSensitive;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
