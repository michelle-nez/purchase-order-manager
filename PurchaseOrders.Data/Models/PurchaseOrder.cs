using System.ComponentModel.DataAnnotations;

namespace PurchaseOrders.Data.Models;

/// <summary>
/// The master row. Deliberately has no Total column - the total is derived from
/// the lines. A stored total can silently disagree with its own line items.
/// </summary>
public class PurchaseOrder
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PO number is required.")]
    [MaxLength(40)]
    [Display(Name = "PO number")]
    public string PoNumber { get; set; } = string.Empty;

    public DateTime OrderedUtc { get; set; } = DateTime.UtcNow;

    [Display(Name = "Expected")]
    public DateTime? ExpectedUtc { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    // Cancelling hides a purchase order. It never deletes the row.
    public bool IsCancelled { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a supplier.")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // The detail rows.
    public List<PurchaseOrderLine> Lines { get; set; } = new();

    // --- Derived from the lines, never stored ---

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public int TotalOrdered => Lines.Sum(l => l.QuantityOrdered);

    public int TotalReceived => Lines.Sum(l => l.QuantityReceived);

    public ReceiptState Receipt =>
        Lines.Count == 0 ? ReceiptState.Empty
        : TotalReceived == 0 ? ReceiptState.Awaiting
        : TotalReceived < TotalOrdered ? ReceiptState.Partial
        : ReceiptState.Complete;
}

public enum ReceiptState
{
    Empty,
    Awaiting,
    Partial,
    Complete
}
