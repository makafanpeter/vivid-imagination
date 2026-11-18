namespace VividImagination.Shared.Contract.Identity;

public static class Constants
{
    public static class StandardScopes
    {
        public const string Role = "role";
        public const string RoleType = "role_type";
        public const string Scopes = "scopes";
        public const string Permissions = "permissions";
        
        // Platform Module Scopes
        public const string IdentityApi = "identity-api";
        public const string FeedApi = "feed-api";
        public const string ModerationApi = "moderation-api";
        public const string SocialApi = "social-api";
        public const string MediaApi = "media-api";


        // Identity Claims
        

        public static IEnumerable<string> GetApiScopes()
        {
            yield return IdentityApi;
            yield return FeedApi;
            yield return ModerationApi;
            yield return SocialApi;
            yield return MediaApi;
        }

        public static Dictionary<string, string> GetAllScopesWithDescription()
        {
            return new Dictionary<string, string>
            {
                { "openid", "OpenID Connect" },
                { "profile", "User profile" },
                { "email", "User email" },
                { "roles", "User roles" },
                { Permissions, "User permissions" },
                { RoleType, "Role type" },
                { IdentityApi, "Identity API access" },
                { FeedApi, "Feed API access" },
                { ModerationApi, "Moderation API access" },
                { SocialApi, "Social API access" }
                
            };
        }
    }
    
    
    public static class ClaimTypes
    {
        public const string TwoFactorEnabled = "two_factor_enabled";
    }
}