using System.ComponentModel.DataAnnotations;

namespace Application.Models.ViewModels.Admin;

public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public string CurrentFilter { get; set; } = "Todos";
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; } = 0;
}