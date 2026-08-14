using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Beneficiary)
            .HasMaxLength(200);

        builder.Property(t => t.Origin)
            .HasMaxLength(200);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(t => t.SavingsAccount)
            .WithMany()
            .HasForeignKey(t => t.SavingsAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.PerformedBy)
            .WithMany()
            .HasForeignKey(t => t.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
