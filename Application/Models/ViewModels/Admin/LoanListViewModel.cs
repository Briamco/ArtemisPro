using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class LoanListViewModel
{
    public List<LoanViewModel> Loans { get; set; } = new();
    public string CurrentFilter { get; set; } = "Activos";
    public string SearchCedula { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; } = 0;
}