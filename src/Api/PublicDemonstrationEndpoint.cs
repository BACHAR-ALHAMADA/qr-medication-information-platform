using MedicationCatalogue.Application;

namespace MedicationInformationPlatform.Api;

public static class PublicDemonstrationEndpoint
{
    public static IResult List(string? language) => !PublicDemonstrationCatalogue.Supports(language ?? "en")
        ? InvalidLanguage() : Results.Ok(PublicDemonstrationCatalogue.List(language ?? "en"));

    public static IResult Resolve(string reference, string? language)
    {
        if (!PublicDemonstrationCatalogue.Supports(language ?? "en")) return InvalidLanguage();
        var product = PublicDemonstrationCatalogue.Resolve(reference, language ?? "en");
        // Invalid, unknown, withdrawn and expired fixtures are deliberately indistinguishable.
        return product is null ? Results.Problem(statusCode: 404, title: "Demonstration unavailable") : Results.Ok(product);
    }

    private static IResult InvalidLanguage() => Results.Problem(statusCode: 400,
        title: "Unsupported demonstration language", detail: "Choose en, fr or ar.");
}
