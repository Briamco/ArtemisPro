using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class AssignCardStep1ViewModel
{
    public decimal AverageSystemDebt { get; set; }
    public string SearchCedula { get; set; } = string.Empty;
    public List<ClientSelectionViewModel> EligibleClients { get; set; } = new();

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string SelectedClientId { get; set; } = string.Empty;
}

public class AssignCardStep2ViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCedula { get; set; } = string.Empty;

    [Required(ErrorMessage = "El límite de crédito es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor que cero.")]
    public decimal CreditLimit { get; set; }
}