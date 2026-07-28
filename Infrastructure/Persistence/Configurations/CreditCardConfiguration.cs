using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("CreditCards");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.CardNumber)
            .IsRequired()
            .HasMaxLength(16);

        builder.HasIndex(cc => cc.CardNumber)
            .IsUnique();

        builder.Property(cc => cc.Limit)
            .HasColumnType("decimal(18,2)");

        builder.Property(cc => cc.Debt)
            .HasColumnType("decimal(18,2)");

        builder.Property(cc => cc.ExpirationDate)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(cc => cc.CvcHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cc => cc.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(cc => cc.Client)
            .WithMany()
            .HasForeignKey(cc => cc.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cc => cc.Admin)
            .WithMany()
            .HasForeignKey(cc => cc.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
