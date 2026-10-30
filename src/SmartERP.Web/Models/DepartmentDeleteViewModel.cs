using System.ComponentModel.DataAnnotations;

public class DepartmentDeleteViewModel
{
    [Required(ErrorMessage = "Invalid department provided.")]
    public double Id { get; set; }
}