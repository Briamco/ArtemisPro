using System.Threading.Tasks;
using Application.DTOs.Banking;

namespace Application.Interfaces.Services;

/// <summary>
/// Servicio de aplicación para las estadísticas del Dashboard del Administrador.
/// </summary>
public interface IAdminDashboardAppService
{
    /// <summary>
    /// Obtiene las estadísticas generales del sistema para el Dashboard del Administrador.
    /// Incluye los 11 indicadores definidos en el documento funcional:
    /// transacciones históricas/del día, pagos históricos/del día,
    /// clientes activos/inactivos, total de productos financieros,
    /// préstamos vigentes, tarjetas activas, cuentas activas y deuda promedio.
    /// </summary>
    Task<AdminDashboardStatsDto> GetGeneralStatsAsync();
}
