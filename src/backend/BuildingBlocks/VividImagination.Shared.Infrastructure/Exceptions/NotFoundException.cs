namespace VividImagination.Shared.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested entity cannot be found.
    /// </summary>
    public class NotFoundException : AppException
    {
        private const string DefaultErrorCode = "Not Found";

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class for an entity with a specific identifier.
        /// </summary>
        /// <param name="entityName">The name of the entity that was not found.</param>
        /// <param name="entityId">The identifier used to search for the entity.</param>
        public NotFoundException(string entityName, object entityId)
            : base($"No {entityName} exists for given id {entityId}.", DefaultErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a custom error message and optional error code.
        /// </summary>
        /// <param name="message">The custom error message.</param>
        /// <param name="code">The error code. Defaults to "Not Found".</param>
        public NotFoundException(string message, string code = DefaultErrorCode) 
            : base(message, code)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class for an entity with a specific identifier and custom message format.
        /// </summary>
        /// <param name="entityName">The name of the entity that was not found.</param>
        /// <param name="entityId">The identifier used to search for the entity.</param>
        /// <param name="messageFormat">The custom message format. Use {0} for entity name and {1} for entity ID.</param>
        public NotFoundException(string entityName, object entityId, string messageFormat)
            : base(string.Format(messageFormat, entityName, entityId), DefaultErrorCode)
        {
        }
    }
}