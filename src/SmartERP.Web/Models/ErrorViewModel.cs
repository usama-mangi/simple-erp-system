using System.ComponentModel.DataAnnotations;

namespace SmartERP.Web.Models;

public class ErrorViewModel
{
    [Required]
    public string Message { get; set; } = string.Empty;

    public bool ShowRequestId => !string.IsNullOrEmpty(Message);
}
