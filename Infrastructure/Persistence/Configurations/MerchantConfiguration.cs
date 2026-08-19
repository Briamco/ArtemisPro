using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(m => m.Email)
            .IsUnique();

        builder.Property(m => m.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.RNC)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(m => m.RNC)
            .IsUnique();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(m => m.Users)
            .WithOne(u => u.Merchant)
            .HasForeignKey(u => u.MerchantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
