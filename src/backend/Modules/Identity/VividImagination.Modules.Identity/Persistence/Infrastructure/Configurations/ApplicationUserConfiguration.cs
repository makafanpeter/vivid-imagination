using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VividImagination.Modules.Identity.Persistence.Entities;
using VividImagination.Shared.Contract.Identity.Enums;

namespace VividImagination.Modules.Identity.Persistence.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(x => x.UserName).HasMaxLength(100);
        builder.HasIndex(x => x.UserName).IsUnique();
        
        builder.HasIndex(u => u.FirstName);
        builder.Property(x => x.FirstName).HasMaxLength(150);
        
        
        builder.HasIndex(u => u.LastName);
        builder.Property(x => x.LastName).HasMaxLength(150);
        
        
        
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(100);
       

       
        builder.HasIndex(u => u.CreatedOn);

        builder.Property(x => x.SafeBrowsingMode)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(SafeBrowsingMode.BlurSensitive);
    }
}
