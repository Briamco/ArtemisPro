using System;
using System.Net;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ArtemisPro.Functions;

public class OverdueLoanInstallmentFunction
{
    private readonly ILoanAppService _loanAppService;
    private readonly ILogger<OverdueLoanInstallmentFunction> _logger;

    public OverdueLoanInstallmentFunction(
        ILoanAppService loanAppService,
        ILogger<OverdueLoanInstallmentFunction> logger)
    {
        _loanAppService = loanAppService;
        _logger = logger;
    }

    /// <summary>
    /// Proceso automático diario para control de cuotas atrasadas.
    /// Según documento funcional: Revisa cuotas vencidas y no pagadas de préstamos activos y las marca como atrasadas.
    /// Cron expression: "0 0 0 * * *" (Ejecución diaria a medianoche UTC).
    /// </summary>
    [Function("ProcessOverdueLoanInstallmentsTimer")]
    public async Task RunTimer([TimerTrigger("%OverdueCheckCronSchedule%")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Iniciando proceso automático diario de cuotas atrasadas: {Timestamp}", DateTime.UtcNow);

        try
        {
            var processedCount = await _loanAppService.ProcessOverdueInstallmentsAsync();
            _logger.LogInformation("Proceso automático finalizado exitosamente. Total de cuotas marcadas en atraso: {Count}", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar el proceso automático de cuotas atrasadas en Azure Functions.");
            throw;
        }
    }

    /// <summary>
    /// Endpoint HTTP para disparo manual o verificación bajo demanda del proceso automático.
    /// </summary>
    [Function("ProcessOverdueLoanInstallmentsHttp")]
    public async Task<HttpResponseData> RunHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "loans/process-overdue")] HttpRequestData req)
    {
        _logger.LogInformation("Iniciando ejecución manual bajo demanda de cuotas atrasadas via HTTP: {Timestamp}", DateTime.UtcNow);

        try
        {
            var processedCount = await _loanAppService.ProcessOverdueInstallmentsAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Proceso completado exitosamente. Cuotas marcadas como atrasadas: {processedCount}",
                processedCount,
                executedAt = DateTime.UtcNow
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar cuotas atrasadas via HTTP.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Ocurrió un error al procesar las cuotas atrasadas.",
                error = ex.Message
            });
            return errorResponse;
        }
    }
}
