using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.BackgroundServices
{
    public class OverdueLoanInstallmentService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueLoanInstallmentService> _logger;

        public OverdueLoanInstallmentService(
            IServiceProvider serviceProvider,
            ILogger<OverdueLoanInstallmentService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var loanAppService = scope.ServiceProvider.GetRequiredService<ILoanAppService>();
                    var processedCount = await loanAppService.ProcessOverdueInstallmentsAsync();
                    if (processedCount > 0)
                    {
                        _logger.LogInformation("Procesadas {Count} cuotas de préstamos vencidas.", processedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating overdue installments in background service");
                }

                // Wait 24 hours before running again
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}

