namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class SystemErrorException(string message, string code = "SYSTEM_ERROR") : AppException(message, code)
    {
        public SystemErrorException() : this("An Unexpected error occured please try again or confirm current operation status")
        {

        }
    }
}