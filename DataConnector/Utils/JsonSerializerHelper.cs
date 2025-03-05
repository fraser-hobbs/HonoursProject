using System.Text.Json;

namespace DataConnector.Utils;

public static class JsonSerializerHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("Deserialization failed.");
    }
}