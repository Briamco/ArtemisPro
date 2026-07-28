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

        builder.Property(m => m.RNC)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(m => m.RNC)
            .IsUnique();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
