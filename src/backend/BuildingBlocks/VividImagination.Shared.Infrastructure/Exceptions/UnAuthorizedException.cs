namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class UnAuthorizedException(string message, string code = "UnAuthorized") : AppException(message, code);
}