namespace MedicationCatalogue.Infrastructure;

public sealed class HealthCanadaDpdOptions
{
    public const string SectionName = "HealthCanadaDpd";
    public string BaseUrl { get; set; } = "https://health-products.canada.ca/api/drug/";
    public int TimeoutSeconds { get; set; } = 15;

    public static bool IsValid(HealthCanadaDpdOptions options) =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.Host == "health-products.canada.ca" &&
        uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.AbsolutePath == "/api/drug/" && options.TimeoutSeconds is >= 1 and <= 30;
}
