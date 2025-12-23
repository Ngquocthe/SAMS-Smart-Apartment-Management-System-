namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO ?? tr? v? thông tin Payment Method
    /// </summary>
    public class PaymentMethodDto
    {
        public Guid PaymentMethodId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool Active { get; set; }
    }

    /// <summary>
    /// DTO ?? t?o m?i Payment Method
    /// </summary>
    public class CreatePaymentMethodDto
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// DTO ?? c?p nh?t Payment Method
  /// </summary>
    public class UpdatePaymentMethodDto
  {
        public string? Name { get; set; }
public bool? Active { get; set; }
    }
}
