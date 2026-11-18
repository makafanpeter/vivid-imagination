using Microsoft.Extensions.Logging;

namespace VividImagination.Shared.Infrastructure.Logging;

public  class LoggingOptions
{
    public Dictionary<string, LogLevel> LogLevel { get; set; }

}