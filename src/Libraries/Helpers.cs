using System.Text.Json;

namespace HttpCrawler.Lib;

public static class Helpers
{
    public static bool IsJsonStringValid(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}
