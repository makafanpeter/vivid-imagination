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
            // Compound index supporting queries for a user's active photos ordered by upload time.
            // Also serves single-field prefix lookups on OwnerId.
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.OwnerId)
                    .Ascending(x => x.Deleted)
                    .Descending(x => x.UploadedTime),
                new CreateIndexOptions { Name = "idx_ownerId_deleted_uploadedTime" }),

            // Compound index supporting active/deleted photo scans ordered by upload time.
            // Subsumes standalone 'Deleted' queries as prefix.
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys
                    .Ascending(x => x.Deleted)
                    .Descending(x => x.UploadedTime),
                new CreateIndexOptions { Name = "idx_deleted_uploadedTime" }),

            // Unique constraint on storage keys across the platform.
            // Sparse = true is mandatory because FileLocation is nullable in FileEntry.
            new CreateIndexModel<FileEntry>(
                Builders<FileEntry>.IndexKeys.Ascending(x => x.FileLocation),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_fileLocation_unique" })
        };

        await FileEntries.Indexes.CreateManyAsync(indexes, ct);
    }
}
