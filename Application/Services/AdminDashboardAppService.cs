using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Banking;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación que implementa la lógica de negocio para calcular
/// las estadísticas generales del Dashboard del Administrador.
/// </summary>
public class AdminDashboardAppService(
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager) : IAdminDashboardAppService
{
    /// <inheritdoc />
    public async Task<AdminDashboardStatsDto> GetGeneralStatsAsync()
    {
        var today = DateTime.Today;

        // ── Transacciones ────────────────────────────────────────────────
        // Incluye TODAS las operaciones financieras registradas en el sistema:
        // depósitos, retiros, transferencias, pagos, avances de efectivo, etc.
        var allTransactions = await unitOfWork.Transactions.GetAllAsync();
        var transactionsList = allTransactions.ToList();

        var totalTransaccionesHistoricas = transactionsList.Count;

        var transaccionesDelDia = transactionsList
            .Count(t => t.Date.Date == today);

        // ── Pagos ────────────────────────────────────────────────────────
        // Solo se consideran pagos las operaciones para abonar/saldar obligaciones:
        //   1. Pagos a tarjetas de crédito (CreditCardTransaction con status Aprobado)
        //   2. Pagos a préstamos (LoanInstallment con PaymentStatus Pagada)

        // Pagos a tarjetas de crédito (aprobados = procesados correctamente)
        var allCreditCardTransactions = await unitOfWork.CreditCardTransactions.GetAllAsync();
        var creditCardPayments = allCreditCardTransactions
            .Where(cct => cct.Status == CreditCardTransactionStatus.Aprobado)
            .ToList();

        var totalPagosTarjetas = creditCardPayments.Count;
        var pagosTarjetasDelDia = creditCardPayments
            .Count(cct => cct.Date.Date == today);

        // Pagos a préstamos (cuotas pagadas)
        var allInstallments = await unitOfWork.LoanInstallments.GetAllAsync();
        var paidInstallments = allInstallments
            .Where(li => li.PaymentStatus == PaymentStatus.Pagada)
            .ToList();

        var totalPagosPrestamos = paidInstallments.Count;
        var pagosPrestamosDia = paidInstallments
            .Count(li => li.DueDate.Date == today);

        var totalPagosHistoricos = totalPagosTarjetas + totalPagosPrestamos;
        var pagosDelDia = pagosTarjetasDelDia + pagosPrestamosDia;

        // ── Clientes ─────────────────────────────────────────────────────
        // Se obtienen los usuarios con rol "Cliente" usando Identity.
        var clientes = await userManager.GetUsersInRoleAsync("Cliente");

        var clientesActivos = clientes.Count(c => c.IsActive);
        var clientesInactivos = clientes.Count(c => !c.IsActive);

        // ── Productos Financieros ────────────────────────────────────────
        // Solo se cuentan productos en estado activo.
        var allSavingsAccounts = await unitOfWork.SavingsAccounts.GetAllAsync();
        var cuentasAhorroActivas = allSavingsAccounts
            .Count(sa => sa.Status == AccountStatus.Activa);

        var allLoans = await unitOfWork.Loans.GetAllAsync();
        var prestamosVigentes = allLoans
            .Count(l => l.Status == LoanStatus.Activo);

        var allCreditCards = await unitOfWork.CreditCards.GetAllAsync();
        var tarjetasCreditoActivas = allCreditCards
            .Count(cc => cc.Status == CardStatus.Activa);

        var totalProductosFinancieros = cuentasAhorroActivas + prestamosVigentes + tarjetasCreditoActivas;

        // ── Deuda Promedio ───────────────────────────────────────────────
        // Fórmula: (Monto pendiente de préstamos activos + Deuda en tarjetas activas)
        //           de clientes activos / Cantidad de clientes activos.
        // Si no hay clientes activos, se retorna 0.
        decimal montoPromedioDeuda = 0m;

        if (clientesActivos > 0)
        {
            var clienteActivoIds = clientes
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToHashSet();

            // Deuda de préstamos activos de clientes activos:
            // Sumamos el monto de las cuotas pendientes (no pagadas)
            var prestamosActivosDeClientes = allLoans
                .Where(l => clienteActivoIds.Contains(l.ClientId) && l.Status == LoanStatus.Activo)
                .ToList();

            decimal deudaPrestamos = 0m;
            foreach (var prestamo in prestamosActivosDeClientes)
            {
                var cuotasPendientes = allInstallments
                    .Where(i => i.LoanId == prestamo.Id && i.PaymentStatus != PaymentStatus.Pagada);
                deudaPrestamos += cuotasPendientes.Sum(i => i.Amount);
            }

            // Deuda de tarjetas de crédito activas de clientes activos
            var deudaTarjetas = allCreditCards
                .Where(cc => clienteActivoIds.Contains(cc.ClientId) && cc.Status == CardStatus.Activa)
                .Sum(cc => cc.Debt);

            var deudaTotal = deudaPrestamos + deudaTarjetas;
            montoPromedioDeuda = Math.Round(deudaTotal / clientesActivos, 2);
        }

        // ── Resultado ────────────────────────────────────────────────────
        return new AdminDashboardStatsDto
        {
            TotalTransaccionesHistoricas = totalTransaccionesHistoricas,
            TransaccionesDelDia = transaccionesDelDia,
            TotalPagosHistoricos = totalPagosHistoricos,
            PagosDelDia = pagosDelDia,
            ClientesActivos = clientesActivos,
            ClientesInactivos = clientesInactivos,
            TotalProductosFinancieros = totalProductosFinancieros,
            PrestamosVigentes = prestamosVigentes,
            TarjetasCreditoActivas = tarjetasCreditoActivas,
            CuentasAhorroActivas = cuentasAhorroActivas,
            MontoPromedioDeuda = montoPromedioDeuda
        };
    }
}
