using System.ComponentModel.DataAnnotations;

namespace PurchaseOrders.Data.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}
