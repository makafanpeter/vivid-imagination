namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class ConflictException(string message, string code = "Conflict") : AppException(message, code);
}
