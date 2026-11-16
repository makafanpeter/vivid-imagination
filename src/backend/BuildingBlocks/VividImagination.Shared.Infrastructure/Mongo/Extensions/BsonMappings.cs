using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace VividImagination.Shared.Infrastructure.Mongo.Extensions;

public static class BsonMappings
{
    private static bool _registered;
    private static readonly object Lock = new();

    public static void Register()
    {
        lock (Lock)
        {
            if (_registered) return;
            
            BsonSerializer.RegisterSerializer(
                new DateTimeOffsetSerializer(BsonType.DateTime));

            BsonSerializer.RegisterSerializer(
                new NullableSerializer<DateTimeOffset>(
                    new DateTimeOffsetSerializer(BsonType.DateTime)));
            
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            
            _registered = true;
        }
    }
    
    public static void RegisterEntity<T>(
        Expression<Func<T, DateTimeOffset>>  createdOn,
        Expression<Func<T, DateTimeOffset?>> updatedOn)
    {
        var nonNullableSerializer = new DateTimeOffsetSerializer(BsonType.DateTime);
        var nullableSerializer    = new NullableSerializer<DateTimeOffset>(nonNullableSerializer);

        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();

           
            cm.GetMemberMap(createdOn)
                .SetSerializer(nonNullableSerializer)
                .SetIsRequired(true);
            
            cm.GetMemberMap(updatedOn)
                .SetSerializer(nullableSerializer)
                .SetIsRequired(false);
        });
    }
    
}