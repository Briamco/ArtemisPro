using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class AssignLoanStep1ViewModel
{
    public decimal AverageSystemDebt { get; set; }
    public string SearchCedula { get; set; } = string.Empty;
    public List<ClientSelectionViewModel> EligibleClients { get; set; } = new();

    [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
    public string SelectedClientId { get; set; } = string.Empty;
}