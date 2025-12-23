using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAMS_BE.Helpers;

/// <summary>
/// JSON converter for DateOnly type
/// </summary>
public class JsonDateOnlyConverter : JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }
        return DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

/// <summary>
/// JSON converter for DateOnly? (nullable) type
/// </summary>
public class JsonNullableDateOnlyConverter : JsonConverter<DateOnly?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(DateFormat));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// JSON converter for TimeOnly type
/// </summary>
public class JsonTimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string TimeFormat = "HH:mm:ss";
    private static readonly string[] SupportedFormats = { "HH:mm:ss", "HH:mm", "H:mm:ss", "H:mm" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        // Thử parse với các format khác nhau
        if (TimeOnly.TryParseExact(value, SupportedFormats, null, System.Globalization.DateTimeStyles.None, out var timeOnly))
        {
            return timeOnly;
        }

        // Nếu không parse được, thử parse với format mặc định
        return TimeOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(TimeFormat));
    }
}

/// <summary>
/// JSON converter for TimeOnly? (nullable) type
/// </summary>
public class JsonNullableTimeOnlyConverter : JsonConverter<TimeOnly?>
{
    private const string TimeFormat = "HH:mm:ss";
    private static readonly string[] SupportedFormats = { "HH:mm:ss", "HH:mm", "H:mm:ss", "H:mm" };

    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Xử lý trường hợp null
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();
        
        // Xử lý empty string hoặc whitespace
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Thử parse với các format khác nhau
        if (TimeOnly.TryParseExact(value, SupportedFormats, null, System.Globalization.DateTimeStyles.None, out var timeOnly))
        {
            return timeOnly;
        }

        // Nếu không parse được, thử parse với format mặc định
        try
        {
            return TimeOnly.Parse(value);
        }
        catch
        {
            // Nếu vẫn không parse được, return null thay vì throw exception
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(TimeFormat));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

