using AutoMapper;
using MongoDB.Bson;

namespace VividImagination.Shared.Application.Mapping;

public class BsonDocumentToObjectConverter 
    :  IValueConverter<BsonDocument?,object?>
{
    
    public  object? Convert(BsonDocument? sourceMember, ResolutionContext context)
    {
        if (sourceMember == null)
            return null;

        try
        {
            // Converts BSON directly to .NET Dictionary
            return BsonTypeMapper.MapToDotNetValue(sourceMember);
        }
        catch
        {
            return null;
        }
    }
}