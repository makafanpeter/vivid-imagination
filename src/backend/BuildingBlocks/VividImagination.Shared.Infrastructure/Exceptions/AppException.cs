namespace VividImagination.Shared.Infrastructure.Exceptions
{
    public class AppException : Exception
    {
        public string Code { get; set; }
        public AppException()
        { }

        public AppException(string message)
            : base(message)
        { }

        public AppException(string message, string code)
            : base(message)
        {
            Code = code;
        }

        public AppException(string message, System.Exception innerException)
            : base(message, innerException)
        { }


        public override string ToString()  => $"Error (Code: {Code}) - {Message}";

        public AppException(string messageFormat, params object[] args)
            : base(string.Format(messageFormat, args))
        {
        }

    }

    
}
