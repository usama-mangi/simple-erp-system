using System.ComponentModel.DataAnnotations;

public class DepartmentDeleteViewModel
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public decimal Id { get; set; }
}