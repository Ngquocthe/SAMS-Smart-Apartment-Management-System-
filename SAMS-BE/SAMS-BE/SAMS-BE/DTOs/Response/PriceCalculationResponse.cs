namespace SAMS_BE.DTOs.Response;

/// <summary>
/// Response cho endpoint calculate price theo package
/// </summary>
public class PriceCalculationResponse
{
    public int BasePrice { get; set; }
    public int TotalPrice { get; set; }
    public string Details { get; set; } = string.Empty;
    public PriceBreakdown? Breakdown { get; set; }
}

/// <summary>
/// Chi tiết phân tích giá
/// </summary>
public class PriceBreakdown
{
    // Cho gói theo tháng
    public int MonthlyPrice { get; set; }
    public int Months { get; set; }
    
    // Chung
    public int BasePrice { get; set; }
    public int Discount { get; set; }
    public int Tax { get; set; }
}

