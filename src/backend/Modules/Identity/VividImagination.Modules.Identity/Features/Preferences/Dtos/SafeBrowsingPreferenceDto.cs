using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Features.Preferences.Dtos;

public sealed record SafeBrowsingPreferenceDto(long UserId, SafeBrowsingMode SafeBrowsingMode, bool IsPersonalisationEnabled = true);

public sealed record UpdatePreferencesRequest(SafeBrowsingMode SafeBrowsingMode, bool? IsPersonalisationEnabled = null);
