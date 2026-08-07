namespace Application.DTOs.Banking;

/// <summary>
/// DTO que contiene las estadísticas generales del Dashboard del Administrador.
/// Incluye los 11 indicadores definidos en el documento funcional.
/// </summary>
public class AdminDashboardStatsDto
{
    /// <summary>
    /// Cantidad total de transacciones registradas en el sistema desde su inicio.
    /// Incluye depósitos, retiros, transferencias, pagos a tarjetas, pagos a préstamos, avances de efectivo, etc.
    /// </summary>
    public int TotalTransaccionesHistoricas { get; set; }

    /// <summary>
    /// Cantidad de transacciones registradas durante la fecha actual del sistema.
    /// </summary>
    public int TransaccionesDelDia { get; set; }

    /// <summary>
    /// Cantidad total de pagos procesados correctamente en todo el historial del sistema.
    /// Solo se consideran pagos las operaciones para abonar/saldar obligaciones
    /// (pagos a tarjetas de crédito y pagos a préstamos).
    /// No se incluyen depósitos, retiros, transferencias ni avances de efectivo.
    /// </summary>
    public int TotalPagosHistoricos { get; set; }

    /// <summary>
    /// Cantidad de pagos a tarjetas de crédito y préstamos procesados correctamente
    /// durante la fecha actual del sistema.
    /// </summary>
    public int PagosDelDia { get; set; }

    /// <summary>
    /// Cantidad de usuarios con rol Cliente que se encuentran en estado Activo.
    /// </summary>
    public int ClientesActivos { get; set; }

    /// <summary>
    /// Cantidad de usuarios con rol Cliente que se encuentran en estado Inactivo.
    /// </summary>
    public int ClientesInactivos { get; set; }

    /// <summary>
    /// Suma total de productos financieros activos asignados a clientes:
    /// Cuentas de ahorro activas + Préstamos activos + Tarjetas de crédito activas.
    /// Excluye productos cancelados, completados o inactivos.
    /// </summary>
    public int TotalProductosFinancieros { get; set; }

    /// <summary>
    /// Cantidad de préstamos activos asignados a clientes.
    /// Excluye préstamos completados/saldados.
    /// </summary>
    public int PrestamosVigentes { get; set; }

    /// <summary>
    /// Cantidad de tarjetas de crédito activas asociadas a clientes.
    /// Excluye tarjetas canceladas.
    /// </summary>
    public int TarjetasCreditoActivas { get; set; }

    /// <summary>
    /// Cantidad total de cuentas de ahorro en estado activo.
    /// Incluye tanto cuentas principales como secundarias; excluye cuentas canceladas.
    /// </summary>
    public int CuentasAhorroActivas { get; set; }

    /// <summary>
    /// Promedio de deuda calculado tomando en cuenta los clientes activos del sistema.
    /// Fórmula: (Deuda total de clientes activos) / (Cantidad de clientes activos).
    /// Si no existen clientes activos, se muestra como 0.
    /// </summary>
    public decimal MontoPromedioDeuda { get; set; }
}
