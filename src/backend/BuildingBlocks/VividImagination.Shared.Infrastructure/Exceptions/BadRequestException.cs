namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class BadRequestException(string message, string code = "Invalid Request") : AppException(message, code);
}