using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Api.BackgroundServices
{
    public class OverdueLoanInstallmentService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OverdueLoanInstallmentService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var pendingInstallments = await unitOfWork.LoanInstallments
                        .FindAsync(i => i.PaymentStatus == PaymentStatus.Pendiente && !i.IsOverdue && i.DueDate < DateTime.UtcNow);

                    if (pendingInstallments.Any())
                    {
                        foreach (var installment in pendingInstallments)
                        {
                            installment.IsOverdue = true;
                            unitOfWork.LoanInstallments.Update(installment);
                        }
                        await unitOfWork.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log exception
                }

                // Wait 24 hours or appropriate time before running again
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
