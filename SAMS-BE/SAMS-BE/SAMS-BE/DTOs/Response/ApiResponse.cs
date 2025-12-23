namespace SAMS_BE.DTOs.Response;

/// <summary>
/// Generic API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; } = true;

    public ApiResponse()
    {
    }

    public ApiResponse(T data, string? message = null)
    {
        Data = data;
        Message = message;
        Success = true;
    }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>(data, message);
    }

    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// API response with pagination info
/// </summary>
public class PagedApiResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; } = true;

    public PagedApiResponse()
    {
    }

    public PagedApiResponse(List<T> data, int totalCount, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        Success = true;
    }
}

