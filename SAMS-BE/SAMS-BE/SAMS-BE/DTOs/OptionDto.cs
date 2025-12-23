namespace SAMS_BE.DTOs;

public class OptionDto
{
    public Guid Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class PagedOptionsDto
{
    public int Total { get; set; }
    public IEnumerable<OptionDto> Items { get; set; } = Enumerable.Empty<OptionDto>();
}



