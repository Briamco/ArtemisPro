using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
{
    public void Configure(EntityTypeBuilder<LoanInstallment> builder)
    {
        builder.ToTable("LoanInstallments");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.InterestAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.CapitalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.PendingBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(li => li.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(li => li.Loan)
            .WithMany(l => l.Installments)
            .HasForeignKey(li => li.LoanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
