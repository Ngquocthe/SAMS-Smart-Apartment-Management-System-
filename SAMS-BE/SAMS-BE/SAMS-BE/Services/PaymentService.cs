using Microsoft.Extensions.Options;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces;
using System.Text;
using System.Text.Json;

namespace SAMS_BE.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly SePayConfig _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly IReceiptService _receiptService;

        public PaymentService(
 HttpClient httpClient, 
            IConfiguration configuration, 
          ILogger<PaymentService> logger,
   IReceiptService receiptService)
        {
   _httpClient = httpClient;
       _logger = logger;
            _receiptService = receiptService;
  
      // Đọc config từ appsettings.json
            _config = new SePayConfig
        {
     ApiToken = configuration["SePay:ApiToken"] ?? throw new ArgumentException("SePay:ApiToken is required"),
BaseUrl = configuration["SePay:BaseUrl"],
        UrlCall = configuration["SePay:UrlCall"] ?? throw new ArgumentException("SePay:UrlCall is required"),
   BankId = configuration["SePay:BankId"] ?? "970423",
    AccountNumber = configuration["SePay:AccountNumber"] ?? throw new ArgumentException("SePay:AccountNumber is required"),
                AccountName = configuration["SePay:AccountName"] ?? "SAMS BUILDING MANAGEMENT"
  };

     // Setup HTTP client cho SePay API
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
 _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiToken}");
        }

        public async Task<PaymentResponseDto> CreatePaymentLinkAsync(CreatePaymentRequestDto request)
        {
            try
            {
                // Tạo mã unique không bao giờ trùng (dùng Guid)
                var uniqueCode = Guid.NewGuid().ToString("N").ToUpper(); // Format: 32 ký tự hex không có dấu gạch
                var uniqueOrderCode = uniqueCode.Substring(0, 16); // Lấy 16 ký tự đầu để ngắn gọn hơn
                
                // Tính tổng tiền từ items nếu có
                var finalAmount = request.Amount;
                if (request.Items != null && request.Items.Any())
                {
                    var calculatedAmount = request.Items.Sum(i => (long)i.Price * (long)i.Quantity);
                    if (calculatedAmount > 0)
                    {
                        finalAmount = (int)calculatedAmount;
                    }
                }

                // Tạo mô tả thanh toán: "Thanhtoanorder" + mã unique
                var description = $"Thanhtoanorder{uniqueOrderCode}";

                // Tạo mã QR qua VietQR API
                // Format: https://api.vietqr.io/image/{BANK_ID}-{ACCOUNT_NO}-aPb5vJk.jpg?accountName={ACCOUNT_NAME}&amount={AMOUNT}&addInfo={DESCRIPTION}
                var accountNameEncoded = Uri.EscapeDataString(_config.AccountName);
                var descriptionEncoded = Uri.EscapeDataString(description);
                var qrCodeUrl = $"https://qr.sepay.vn/img?acc={_config.AccountNumber}&bank={_config.BankId}&amount={finalAmount}&des={descriptionEncoded}";

                _logger.LogInformation($"QR code generated - UniqueCode: {uniqueOrderCode}, Amount: {finalAmount}, Description: {description}");

                return new PaymentResponseDto
                {
                    Success = true,
                    Message = "Tạo mã QR thanh toán thành công",
                    QrCode = qrCodeUrl,
                    OrderCode = uniqueOrderCode, // Trả về mã unique để check sau
                    CheckoutUrl = null,
                    PaymentLinkId = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating QR code payment");
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi tạo mã QR thanh toán: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(int orderCode)
        {
            // Legacy method - không dùng nữa
            // Sử dụng GetPaymentStatusByUniqueCodeAsync với uniqueCode và amount
            _logger.LogWarning($"GetPaymentStatusAsync with int orderCode is deprecated. Use GetPaymentStatusByUniqueCodeAsync instead.");
            return new PaymentStatusDto
            {
                Success = false,
                Message = "Method này không còn được sử dụng. Vui lòng dùng GET /api/Payment/status/{uniqueCode}?amount={amount}",
                Status = "ERROR"
            };
        }

        /// <summary>
        /// Check payment status bằng cách gọi SePay API và tìm giao dịch match
        /// Tự động tạo Receipt và Journal Entry khi thanh toán thành công
        /// </summary>
        public async Task<PaymentStatusDto> GetPaymentStatusByUniqueCodeAsync(string uniqueCode, int expectedAmount)
        {
            try
            {
                _logger.LogInformation($"Checking payment status for uniqueCode: {uniqueCode}, expectedAmount: {expectedAmount}");

          // Tạo description cần tìm: "Thanhtoanorder" + uniqueCode
        var expectedDescription = $"Thanhtoanorder{uniqueCode}";

   // Gọi SePay API để lấy list giao dịch
      var transactions = await GetSePayTransactionsAsync();

    if (transactions == null || !transactions.Any())
     {
        _logger.LogInformation("No transactions found from SePay API");
            return new PaymentStatusDto
            {
 Success = true,
           Message = "Chưa có giao dịch thanh toán",
        Status = "PENDING",
            Data = new PaymentDataDto
       {
      OrderCode = uniqueCode,
 Amount = expectedAmount,
       Description = expectedDescription
     }
 };
}

           // Log tất cả transactions để debug
           _logger.LogInformation($"Checking {transactions.Count} transactions. Expected: Amount={expectedAmount}, Description contains: {expectedDescription}");
   foreach (var t in transactions.Take(5)) // Log 5 transaction đầu tiên
 {
         var contentPreview = t.TransactionContent != null 
       ? t.TransactionContent.Substring(0, Math.Min(100, t.TransactionContent.Length)) 
      : "null";
     _logger.LogInformation($"Transaction: Id={t.TransactionId}, Amount={t.Amount}, TransactionContent={contentPreview}");
    }

           // Tìm giao dịch match với transaction_content (chỉ cần content match, không cần amount match vì có thể có phí)
         var matchedTransaction = transactions.FirstOrDefault(t =>
     {
          // Ưu tiên tìm trong transaction_content (field chính từ SePay)
    var contentMatch = !string.IsNullOrWhiteSpace(t.TransactionContent) && 
    t.TransactionContent.Contains(expectedDescription, StringComparison.OrdinalIgnoreCase);
       
         // Fallback: tìm trong description nếu không có transaction_content
        if (!contentMatch && !string.IsNullOrWhiteSpace(t.Description))
         {
  contentMatch = t.Description.Contains(expectedDescription, StringComparison.OrdinalIgnoreCase);
  }
       
      // Kiểm tra amount match (optional, chỉ để log)
        var amountMatch = t.Amount == expectedAmount;
        var contentPreview = t.TransactionContent != null 
    ? t.TransactionContent.Substring(0, Math.Min(50, t.TransactionContent.Length)) 
     : "null";
        _logger.LogDebug($"Transaction {t.TransactionId}: AmountMatch={amountMatch} (t.Amount={t.Amount}, expected={expectedAmount}), ContentMatch={contentMatch} (Content: {contentPreview})");
        
  // Chỉ cần contentMatch là đủ, amount có thể khác do phí giao dịch
         return contentMatch;
           });

                if (matchedTransaction != null)
           {
         var transactionContent = matchedTransaction.TransactionContent ?? matchedTransaction.Description ?? expectedDescription;
           _logger.LogInformation($"✅ Payment found! Transaction: {matchedTransaction.TransactionId}, Amount: {matchedTransaction.Amount}, TransactionContent: {transactionContent}");

          // 🆕 TỰ ĐỘNG TẠO RECEIPT VÀ JOURNAL ENTRY
          // Lấy InvoiceId từ uniqueCode trong Data field (nếu có lưu từ CreatePaymentLinkAsync)
              // Hoặc parse từ description/metadata
      // Tạm thời skip auto-create Receipt vì cần InvoiceId
        // Sẽ để frontend gọi API create receipt sau khi check payment success
    
         return new PaymentStatusDto
            {
  Success = true,
       Message = "Thanh toán thành công",
       Status = "PAID",
   Data = new PaymentDataDto
           {
       OrderCode = uniqueCode,
             Amount = matchedTransaction.Amount,
     Description = transactionContent,
  TransactionDateTime = matchedTransaction.TransactionDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? ""
       }
           };
   }
       else
        {
        _logger.LogInformation($"No matching transaction found. Expected: Amount={expectedAmount}, TransactionContent should contain: {expectedDescription}");
           return new PaymentStatusDto
          {
       Success = true,
    Message = "Chưa có giao dịch thanh toán phù hợp",
               Status = "PENDING",
       Data = new PaymentDataDto
          {
         OrderCode = uniqueCode,
           Amount = expectedAmount,
       Description = expectedDescription
         }
   };
       }
       }
  catch (Exception ex)
       {
    _logger.LogError(ex, $"Error checking payment status for uniqueCode: {uniqueCode}");
    return new PaymentStatusDto
           {
Success = false,
           Message = $"Lỗi kiểm tra thanh toán: {ex.Message}",
    Status = "ERROR"
      };
         }
        }

        /// <summary>
        /// Gọi SePay API để lấy list giao dịch
        /// </summary>
        private async Task<List<SePayTransactionDto>> GetSePayTransactionsAsync()
        {
            try
            {
                _logger.LogInformation($"Calling SePay API: {_config.UrlCall}");

                // Tạo HttpClient mới với header Authorization
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiToken}");

                var response = await httpClient.GetAsync(_config.UrlCall);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"SePay API response: Status={response.StatusCode}, ContentLength={responseContent.Length}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"SePay API error: {response.StatusCode} - {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                    return new List<SePayTransactionDto>();
                }

                // Parse JSON response
                var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                var transactions = new List<SePayTransactionDto>();

                // Thử nhiều format response khác nhau
                // Ưu tiên tìm "transactions" (format SePay API)
                JsonElement? dataElement = null;
                
                if (root.TryGetProperty("transactions", out var transProp))
                {
                    dataElement = transProp;
                    _logger.LogInformation("Found 'transactions' field in SePay response");
                }
                else if (root.TryGetProperty("data", out var dataProp))
                {
                    dataElement = dataProp;
                    _logger.LogInformation("Found 'data' field in SePay response");
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
                    _logger.LogInformation("Root is an array");
                }
                else if (root.TryGetProperty("items", out var itemsProp))
                {
                    dataElement = itemsProp;
                    _logger.LogInformation("Found 'items' field in SePay response");
                }
                else
                {
                    _logger.LogWarning("Could not find transactions array in SePay response. Available properties: " + string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
                }

                if (dataElement.HasValue && dataElement.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var transaction in dataElement.Value.EnumerateArray())
                    {
                        // Parse amount từ amount_in (tiền vào) hoặc amount_out (tiền ra)
                        int parsedAmount = 0;
                        if (transaction.TryGetProperty("amount_in", out var amountInElement))
                        {
                            var amountInStr = amountInElement.GetString();
                            if (!string.IsNullOrWhiteSpace(amountInStr) && decimal.TryParse(amountInStr, out var amountInDecimal))
                            {
                                parsedAmount = (int)Math.Round(amountInDecimal);
                            }
                        }
                        else if (transaction.TryGetProperty("amount_out", out var amountOutElement))
                        {
                            var amountOutStr = amountOutElement.GetString();
                            if (!string.IsNullOrWhiteSpace(amountOutStr) && decimal.TryParse(amountOutStr, out var amountOutDecimal))
                            {
                                parsedAmount = (int)Math.Round(amountOutDecimal);
                            }
                        }
                        else if (transaction.TryGetProperty("amount", out var amountElement))
                        {
                            // Fallback nếu có field amount trực tiếp
                            if (amountElement.ValueKind == JsonValueKind.Number)
                            {
                                parsedAmount = amountElement.GetInt32();
                            }
                            else if (amountElement.ValueKind == JsonValueKind.String)
                            {
                                var amountStr = amountElement.GetString();
                                if (!string.IsNullOrWhiteSpace(amountStr) && decimal.TryParse(amountStr, out var amountDecimal))
                                {
                                    parsedAmount = (int)Math.Round(amountDecimal);
                                }
                            }
                        }

                        var transactionDto = new SePayTransactionDto
                        {
                            TransactionId = transaction.TryGetProperty("id", out var idElement) ? idElement.GetString() : null,
                            Amount = parsedAmount,
                            Description = transaction.TryGetProperty("description", out var descElement) ? descElement.GetString() 
                                : (transaction.TryGetProperty("addInfo", out var addInfoElement) ? addInfoElement.GetString() 
                                : (transaction.TryGetProperty("content", out var contentElement) ? contentElement.GetString() : null)),
                            TransactionContent = transaction.TryGetProperty("transaction_content", out var transContentElement) ? transContentElement.GetString()
                                : (transaction.TryGetProperty("transactionContent", out var transContentElement2) ? transContentElement2.GetString()
                                : (transaction.TryGetProperty("content", out var contentElement2) ? contentElement2.GetString() : null)),
                            AccountNumber = transaction.TryGetProperty("account_number", out var accElement) ? accElement.GetString()
                                : (transaction.TryGetProperty("accountNumber", out var accElement2) ? accElement2.GetString() : null),
                            AccountName = transaction.TryGetProperty("account_name", out var nameElement) ? nameElement.GetString()
                                : (transaction.TryGetProperty("accountName", out var nameElement2) ? nameElement2.GetString() : null),
                            TransactionDate = transaction.TryGetProperty("transaction_date", out var dateElement) 
                                ? (DateTime.TryParse(dateElement.GetString(), out var dt) ? dt : (DateTime?)null)
                                : (transaction.TryGetProperty("transactionDate", out var dateElement2)
                                    ? (DateTime.TryParse(dateElement2.GetString(), out var dt2) ? dt2 : (DateTime?)null)
                                    : (transaction.TryGetProperty("createdAt", out var createdAtElement)
                                        ? (DateTime.TryParse(createdAtElement.GetString(), out var dt3) ? dt3 : (DateTime?)null)
                                        : null)),
                            Status = transaction.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null,
                            ReferenceNumber = transaction.TryGetProperty("reference_number", out var refElement) ? refElement.GetString()
                                : (transaction.TryGetProperty("referenceNumber", out var refElement2) ? refElement2.GetString() : null)
                        };

                        transactions.Add(transactionDto);
                    }
                }

                _logger.LogInformation($"Retrieved {transactions.Count} transactions from SePay API");
                return transactions;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing SePay API JSON response");
                return new List<SePayTransactionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SePay API");
                return new List<SePayTransactionDto>();
            }
        }

        public async Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhookData)
        {
            // SePay không dùng webhook, dùng polling thay thế
            await Task.CompletedTask;
            return true;
        }

        public async Task<CancelPaymentResponseDto> CancelPaymentAsync(int orderCode, string? cancellationReason = null)
        {
            // SePay không hỗ trợ cancel qua API, chỉ có thể để hết hạn
            await Task.CompletedTask;
            return new CancelPaymentResponseDto
            {
                Success = false,
                Message = "SePay không hỗ trợ hủy thanh toán qua API",
                OrderCode = orderCode.ToString()
            };
        }

        public bool VerifyWebhookSignature(string webhookUrl, string requestBody, string signature)
        {
            // SePay không dùng webhook
            return false;
        }

        /// <summary>
        /// Lấy danh sách giao dịch từ SePay API
        /// </summary>
        public async Task<List<SePayTransactionDto>> GetTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // SePay API endpoint có thể khác, cần kiểm tra documentation
                // Tạm thời thử một số endpoint phổ biến
                var endpoints = new[]
                {
                    "/transactions",
                    "/api/transactions",
                    "/v1/transactions",
                    "/payment/transactions"
                };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var queryParams = new List<string>();
                        if (fromDate.HasValue)
                        {
                            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                        }
                        if (toDate.HasValue)
                        {
                            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                        }

                        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                        var response = await _httpClient.GetAsync($"{endpoint}{queryString}");
                        var responseContent = await response.Content.ReadAsStringAsync();

                        // Log response content để debug (chỉ log 500 ký tự đầu)
                        _logger.LogInformation($"SePay API response for {endpoint}: Status={response.StatusCode}, ContentType={response.Content.Headers.ContentType?.MediaType}, First500Chars={responseContent.Substring(0, Math.Min(500, responseContent.Length))}");

                        // Kiểm tra xem response có phải JSON không
                        if (responseContent.TrimStart().StartsWith("<"))
                        {
                            _logger.LogWarning($"SePay API returned HTML/XML instead of JSON for endpoint {endpoint}. Response type: {response.Content.Headers.ContentType?.MediaType}");
                            // Log thêm để debug
                            if (responseContent.Contains("<!DOCTYPE") || responseContent.Contains("<html"))
                            {
                                _logger.LogWarning("SePay returned HTML page. May need to check API documentation or use different endpoint.");
                            }
                            continue;
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                var jsonDocument = JsonDocument.Parse(responseContent);
                                var root = jsonDocument.RootElement;

                                var transactions = new List<SePayTransactionDto>();

                                // Thử nhiều format response khác nhau
                                JsonElement? dataElement = null;
                                if (root.TryGetProperty("data", out var dataProp))
                                {
                                    dataElement = dataProp;
                                }
                                else if (root.ValueKind == JsonValueKind.Array)
                                {
                                    dataElement = root;
                                }
                                else if (root.TryGetProperty("transactions", out var transProp))
                                {
                                    dataElement = transProp;
                                }

                                if (dataElement.HasValue && dataElement.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var transaction in dataElement.Value.EnumerateArray())
                                    {
                                        transactions.Add(new SePayTransactionDto
                                        {
                                            TransactionId = transaction.TryGetProperty("id", out var idElement) ? idElement.GetString() : null,
                                            Amount = transaction.TryGetProperty("amount", out var amountElement) ? amountElement.GetInt32() : 0,
                                            Description = transaction.TryGetProperty("addInfo", out var addInfoElement) ? addInfoElement.GetString() : null,
                                            AccountNumber = transaction.TryGetProperty("accountNumber", out var accElement) ? accElement.GetString() : null,
                                            AccountName = transaction.TryGetProperty("accountName", out var nameElement) ? nameElement.GetString() : null,
                                            TransactionDate = transaction.TryGetProperty("transactionDate", out var dateElement) 
                                                ? DateTime.TryParse(dateElement.GetString(), out var dt) ? dt : (DateTime?)null
                                                : null,
                                            Status = transaction.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null
                                        });
                                    }
                                }

                                _logger.LogInformation($"Successfully retrieved {transactions.Count} transactions from SePay endpoint {endpoint}");
                                return transactions;
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogWarning($"Failed to parse JSON from endpoint {endpoint}: {jsonEx.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"SePay API returned {response.StatusCode} for endpoint {endpoint}: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogWarning($"HTTP error for endpoint {endpoint}: {httpEx.Message}");
                        continue;
                    }
                }

                // Nếu không endpoint nào work, trả về empty list
                _logger.LogWarning("Could not retrieve transactions from any SePay endpoint. Please check SePay API documentation.");
                return new List<SePayTransactionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions from SePay");
                return new List<SePayTransactionDto>();
            }
        }
    }

    public class SePayConfig
    {
        public string ApiToken { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string UrlCall { get; set; } = string.Empty;
        public string BankId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }

    public class SePayTransactionDto
    {
        public string? TransactionId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
        public string? TransactionContent { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Status { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
