using System.ComponentModel.DataAnnotations;

namespace PurchaseOrders.Data.Models;

/// <summary>
/// A detail row. Cannot exist without its parent purchase order.
/// </summary>
public class PurchaseOrderLine
{
    public int Id { get; set; }

    [Required(ErrorMessage = "SKU is required.")]
    [MaxLength(40)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Order at least 1.")]
    [Display(Name = "Ordered")]
    public int QuantityOrdered { get; set; } = 1;

    [Range(0, 100000, ErrorMessage = "Received cannot be negative.")]
    [Display(Name = "Received")]
    public int QuantityReceived { get; set; }

    // decimal, not double - money must be exact.
    [Range(0, 999999, ErrorMessage = "Unit cost must be between 0 and 999,999.")]
    [Display(Name = "Unit cost")]
    public decimal UnitCost { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    // Derived, never stored.
    public decimal LineTotal => QuantityOrdered * UnitCost;

    public int Outstanding => QuantityOrdered - QuantityReceived;

    public bool IsFullyReceived => QuantityReceived >= QuantityOrdered;
}
