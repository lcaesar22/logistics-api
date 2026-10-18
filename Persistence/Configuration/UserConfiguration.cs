using Logistics.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logistics.Api.Persistence.Configuration;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
     public void Configure(EntityTypeBuilder<User> builder)
     {
          builder.ToTable("Users");
          builder.HasKey(x => x.Id);
          builder.Property(x => x.Email).HasMaxLength(256);
          builder.HasIndex(x => x.Email).IsUnique();
          builder.Property(x => x.PasswordHash).HasMaxLength(256);
          builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
     }
}