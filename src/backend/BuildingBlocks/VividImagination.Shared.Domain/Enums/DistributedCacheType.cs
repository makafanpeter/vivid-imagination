using System.Runtime.Serialization;

namespace VividImagination.Shared.Domain.Enums;

public enum DistributedCacheType
{
    [EnumMember(Value = "memory")]
    Memory,
    [EnumMember(Value = "redis")]
    Redis
}