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
        public const string ProviderApi = "provider-api";
        public const string BookingApi = "booking-api";
        public const string ReputationApi = "reputation-api";
        public const string StorageApi = "storage-api";


        // Identity Claims
        

        public static IEnumerable<string> GetApiScopes()
        {
            yield return IdentityApi;
            yield return ProviderApi;
            yield return BookingApi;
            yield return ReputationApi;
            yield return StorageApi;
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
                { ProviderApi, "Provider API access" },
                { BookingApi, "Booking API access" },
                { ReputationApi, "Reputation API access" }
                
            };
        }
    }
    
    
    public static class ClaimTypes
    {
        public const string TwoFactorEnabled = "two_factor_enabled";
    }
}