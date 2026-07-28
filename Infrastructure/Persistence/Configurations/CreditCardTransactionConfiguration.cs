using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CreditCardTransactionConfiguration : IEntityTypeConfiguration<CreditCardTransaction>
{
    public void Configure(EntityTypeBuilder<CreditCardTransaction> builder)
    {
        builder.ToTable("CreditCardTransactions");

        builder.HasKey(cct => cct.Id);

        builder.Property(cct => cct.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(cct => cct.MerchantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cct => cct.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(cct => cct.CreditCard)
            .WithMany(cc => cc.Transactions)
            .HasForeignKey(cct => cct.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
