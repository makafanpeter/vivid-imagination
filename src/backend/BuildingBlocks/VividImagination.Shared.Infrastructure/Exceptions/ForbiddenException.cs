namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string? user) : base($"UnauthorizedAccess: {user} is not allowed to access this resource.", "Access Denied")
        {

        }
        
        public ForbiddenException() : base($"UnauthorizedAccess: user is not allowed to access this resource.", "Access Denied")
        {

        }
    }
}