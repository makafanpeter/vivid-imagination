namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class ServiceUnavailableException(
        string message = "An Unexpected error occured please try again or confirm current operation status",
        string code = "Service Unavailable") : AppException(message, code);
}
