using Microsoft.AspNetCore.Identity;
using VividImagination.Shared.Contract.Identity.Enums;
using VividImagination.Shared.Domain.Entities;

namespace VividImagination.Modules.Identity.Persistence.Entities;

public class User: IdentityUser<long>, IHasKey<long>, IAuditable
{
    public SafeBrowsingMode SafeBrowsingMode { get; set; } = SafeBrowsingMode.BlurSensitive;
    public bool IsPersonalisationEnabled { get; set; } = true;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
