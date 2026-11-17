using Humanizer;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VividImagination.Modules.Media.Persistence.Entities;
using VividImagination.Shared.Infrastructure.Mongo;

namespace VividImagination.Modules.Media.Persistence;


public class MediaDbContext : MongoDbContext
{
    public MediaDbContext(IOptions<MongoOptions> options) : base(options)
    {
        FileEntries = GetCollection<FileEntry>(nameof(FileEntry).Pluralize());
    }

    public IMongoCollection<FileEntry> FileEntries { get; set; }


    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Ascending(x => x.OwnerId),
                new CreateIndexOptions { Name = "idx_ownerId" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Ascending(x => x.Deleted),
                new CreateIndexOptions { Name = "idx_deleted" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.OwnerId)
                    .Ascending(x => x.Deleted)
                    .Descending(x => x.UploadedTime),
                new CreateIndexOptions { Name = "idx_ownerId_deleted_uploadedTime" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.Deleted)
                    .Descending(x => x.UploadedTime),
                new CreateIndexOptions { Name = "idx_organizationId_deleted_uploadedTime" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.ContainerName)
                    .Ascending(x => x.Deleted),
                new CreateIndexOptions { Name = "idx_containerName_deleted" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.Deleted)
                    .Descending(x => x.UploadedTime),
                new CreateIndexOptions { Name = "idx_public_deleted_uploadedTime" }),


            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.OwnerId)
                    .Ascending(x => x.FileExtension)
                    .Ascending(x => x.Deleted),
                new CreateIndexOptions { Name = "idx_ownerId_fileExtension_deleted" }),

            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Ascending(x => x.FileLocation),
                new CreateIndexOptions { Unique = true, Name = "idx_fileLocation_unique" }),
            
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Descending(x => x.CreatedOn),
                new CreateIndexOptions { Name = "idx_createdOn" }),
        
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Descending(x => x.UpdatedOn),
                new CreateIndexOptions
                {
                    Name = "idx_updatedOn",
                    Sparse = true 
                }),
        };

        await FileEntries.Indexes.CreateManyAsync(indexes, ct);
    }
}
