using SAMS_BE.Models;

namespace SAMS_BE.Helpers
{
    public static class InvoiceCalculationHelper
    {
        /// <summary>
      /// Calculate subtotal, tax, and total amounts for an invoice based on its details
        /// </summary>
        public static (decimal Subtotal, decimal Tax, decimal Total) CalculateInvoiceTotals(IEnumerable<InvoiceDetail> details)
  {
    var subtotal = details.Sum(d => d.Amount ?? 0);
     var tax = details.Sum(d => d.VatAmount ?? 0);
         var total = subtotal + tax;

    return (subtotal, tax, total);
        }

        /// <summary>
    /// Calculate amount and VAT for a single detail
        /// </summary>
   public static (decimal Amount, decimal VatAmount) CalculateDetailAmounts(decimal quantity, decimal unitPrice, decimal? vatRate)
        {
  var amount = quantity * unitPrice;
            var vatAmount = vatRate.HasValue ? amount * (vatRate.Value / 100) : 0;
            return (amount, vatAmount);
  }

        /// <summary>
  /// Update invoice totals based on its details
   /// </summary>
     public static void UpdateInvoiceTotals(Invoice invoice, IEnumerable<InvoiceDetail> details)
   {
 var (subtotal, tax, total) = CalculateInvoiceTotals(details);
invoice.SubtotalAmount = subtotal;
            invoice.TaxAmount = tax;
    invoice.TotalAmount = total;
        }
    }
}
