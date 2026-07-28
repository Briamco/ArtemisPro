using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Cedula)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(u => u.Cedula)
            .IsUnique();

        builder.Property(u => u.Email)
            .HasMaxLength(200);

        builder.Property(u => u.UserName)
            .HasMaxLength(100);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(false);
    }
}
