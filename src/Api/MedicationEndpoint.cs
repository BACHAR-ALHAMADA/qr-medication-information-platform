using MedicationCatalogue.Application;

namespace MedicationInformationPlatform.Api;

public static class MedicationEndpoint
{
    public static async Task<IResult> LookupAsync(string din, string? language, IMedicationProductLookup lookup, CancellationToken cancellationToken)
    {
        language ??= "en";
        if (!MedicationLookupInput.IsValid(din, language))
            return Results.Problem(statusCode: 400, title: "Invalid lookup", detail: "Provide an eight-digit DIN and language en or fr.");
        var result = await lookup.LookupAsync(din, language, cancellationToken);
        return result.Outcome switch
        {
            MedicationLookupOutcome.Found when result.Product is not null => Results.Ok(result.Product),
            MedicationLookupOutcome.NotFound => Results.Problem(statusCode: 404, title: "Product not found"),
            MedicationLookupOutcome.InvalidInput => Results.Problem(statusCode: 400, title: "Invalid lookup"),
            MedicationLookupOutcome.Timeout => Results.Problem(statusCode: 504, title: "Medication source timed out"),
            _ => Results.Problem(statusCode: 502, title: "Medication source unavailable", detail: "Complete product metadata could not be retrieved.")
        };
    }
}
