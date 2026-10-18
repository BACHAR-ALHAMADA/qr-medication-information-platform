namespace MedicationCatalogue.Application;

public interface IMedicationProductLookup
{
    Task<MedicationLookupResult> LookupAsync(string din, string language, CancellationToken cancellationToken);
}

public static class MedicationLookupInput
{
    public static bool IsValid(string? din, string? language) =>
        din is { Length: 8 } && din.All(c => c is >= '0' and <= '9') && language is "en" or "fr";
}

public enum MedicationLookupOutcome { Found, NotFound, InvalidInput, UpstreamFailure, InvalidUpstreamData, Timeout }

public sealed record MedicationLookupResult(MedicationLookupOutcome Outcome, MedicationProductMetadata? Product = null);

public sealed record MedicationProductMetadata(
    string Din,
    int DpdDrugCode,
    string BrandName,
    string CompanyName,
    string ProductClass,
    string? Descriptor,
    DateOnly? SourceLastUpdateDate,
    IReadOnlyList<MedicationIngredient> ActiveIngredients,
    IReadOnlyList<string> DosageForms,
    IReadOnlyList<string> Routes,
    MedicationProductStatus Status,
    MedicationSourceProvenance Source)
{
    public string ContentClassification => "RegulatoryProductMetadata";
    public bool IsApprovedPatientGuidance => false;
    public bool IsPublished => false;
    public string Notice => "Source product metadata only. Not patient-specific instructions or approved patient guidance. Ingredient strength is not a dosing recommendation.";
}

public sealed record MedicationIngredient(string Name, string Strength, string StrengthUnit, string? DosageValue, string? DosageUnit);
public sealed record MedicationProductStatus(string Name, int Code, DateOnly? HistoryDate, DateOnly? OriginalMarketDate);
public sealed record MedicationSourceProvenance(string Provider, Uri ApiBaseUrl, Uri ProductPageUrl, string Language, DateTimeOffset RetrievedAtUtc)
{
    public string Attribution => "Contains information licensed under the Open Government Licence – Canada.";
    public Uri LicenceUrl => new("https://open.canada.ca/en/open-government-licence-canada");
}
