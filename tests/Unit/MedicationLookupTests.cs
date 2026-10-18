using System.Net;
using System.Text;
using MedicationCatalogue.Application;
using MedicationCatalogue.TestFixtures;
using MedicationCatalogue.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace MedicationCatalogue.UnitTests;

public sealed class MedicationLookupTests
{
    [Theory]
    [InlineData("00001234", "en", true)]
    [InlineData("00001234", "fr", true)]
    [InlineData("1234", "en", false)]
    [InlineData("１２３４５６７８", "en", false)]
    [InlineData("00001234", "ar", false)]
    [InlineData("https://example.test", "en", false)]
    public void Validates_exact_ascii_din_and_supported_language(string din, string language, bool expected) =>
        Assert.Equal(expected, MedicationLookupInput.IsValid(din, language));

    [Fact]
    public async Task Normalizes_objects_arrays_and_optional_nulls_without_confusing_identifiers()
    {
        var handler = new DpdStubHandler();
        var result = await Create(handler).LookupAsync("00001234", "fr", default);
        var product = Assert.IsType<MedicationProductMetadata>(result.Product);
        Assert.Equal(MedicationLookupOutcome.Found, result.Outcome);
        Assert.Equal("00001234", product.Din);
        Assert.Equal(42, product.DpdDrugCode);
        Assert.Equal("EXAMPLE PRODUCT", product.BrandName);
        Assert.Null(product.Descriptor);
        Assert.Null(product.SourceLastUpdateDate);
        Assert.Null(product.Status.OriginalMarketDate);
        Assert.Null(product.ActiveIngredients[0].DosageUnit);
        Assert.Equal("MG", product.ActiveIngredients[0].StrengthUnit);
        Assert.Equal("Tablet", Assert.Single(product.DosageForms));
        Assert.Equal("Oral", Assert.Single(product.Routes));
        Assert.All(handler.Requests.Skip(1), uri => Assert.Contains("id=42&lang=fr", uri.Query));
        Assert.Contains("din=00001234&lang=fr", handler.Requests[0].Query);
        Assert.All(handler.Requests, uri => Assert.Equal("health-products.canada.ca", uri.Host));
        Assert.Contains("code=42&lang=fre", product.Source.ProductPageUrl.Query);
        Assert.Equal("fr", product.Source.Language);
        Assert.False(product.IsApprovedPatientGuidance);
        Assert.False(product.IsPublished);
    }

    [Fact]
    public async Task Preserves_all_ingredients_and_liquid_denominator_without_deriving_a_dose()
    {
        var handler = new DpdStubHandler();
        handler.Responses["drugproduct"] = DpdStubHandler.Product.Replace("\"number_of_ais\":\"1\"", "\"number_of_ais\":\"2\"");
        handler.Responses["activeingredient"] = """
            [{"drug_code":42,"ingredient_name":"EXAMPLE A","strength":"2.5","strength_unit":"MG","dosage_value":"5","dosage_unit":"ML"},
             {"drug_code":42,"ingredient_name":"EXAMPLE B","strength":"1","strength_unit":"MG","dosage_value":"5","dosage_unit":"ML"}]
            """;
        var result = await Create(handler).LookupAsync("00001234", "en", default);
        Assert.Equal(2, result.Product!.ActiveIngredients.Count);
        Assert.Equal("2.5", result.Product.ActiveIngredients[0].Strength);
        Assert.Equal("5", result.Product.ActiveIngredients[0].DosageValue);
        Assert.Equal("ML", result.Product.ActiveIngredients[0].DosageUnit);
    }

    [Theory]
    [InlineData("drugproduct", "[]", MedicationLookupOutcome.NotFound)]
    [InlineData("drugproduct", "null", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("drugproduct", "not json", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("drugproduct", "[null]", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("activeingredient", "[]", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("form", "[]", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("route", "{}", MedicationLookupOutcome.InvalidUpstreamData)]
    [InlineData("status", "[]", MedicationLookupOutcome.InvalidUpstreamData)]
    public async Task Handles_missing_or_malformed_resource(string resource, string json, MedicationLookupOutcome expected)
    {
        var handler = new DpdStubHandler();
        handler.Responses[resource] = json;
        var result = await Create(handler).LookupAsync("00001234", "en", default);
        Assert.Equal(expected, result.Outcome);
        Assert.Null(result.Product);
    }

    [Theory]
    [InlineData("drugproduct", "drug_identification_number", "99991234")]
    [InlineData("drugproduct", "brand_name", "")]
    [InlineData("drugproduct", "company_name", "")]
    [InlineData("drugproduct", "class_name", "")]
    [InlineData("activeingredient", "ingredient_name", "")]
    [InlineData("activeingredient", "strength", "")]
    [InlineData("activeingredient", "strength_unit", "")]
    public async Task Rejects_wrong_din_and_required_empty_fields(string resource, string field, string replacement)
    {
        var handler = new DpdStubHandler();
        var text = handler.Responses[resource];
        handler.Responses[resource] = System.Text.RegularExpressions.Regex.Replace(text,
            $"\"{field}\":\"[^\"]*\"", $"\"{field}\":\"{replacement}\"");
        Assert.Equal(MedicationLookupOutcome.InvalidUpstreamData, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
    }

    [Theory]
    [InlineData("activeingredient")]
    [InlineData("form")]
    [InlineData("route")]
    [InlineData("status")]
    public async Task Rejects_cross_product_related_rows(string resource)
    {
        var handler = new DpdStubHandler();
        handler.Responses[resource] = handler.Responses[resource].Replace("\"drug_code\":42", "\"drug_code\":43");
        var result = await Create(handler).LookupAsync("00001234", "en", default);
        Assert.Equal(MedicationLookupOutcome.InvalidUpstreamData, result.Outcome);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task Rejects_multiple_product_matches_even_if_one_din_is_correct()
    {
        var handler = new DpdStubHandler();
        handler.Responses["drugproduct"] = $"[{DpdStubHandler.Product},{DpdStubHandler.Product}]";
        Assert.Equal(MedicationLookupOutcome.InvalidUpstreamData, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
    }

    [Theory]
    [InlineData("drugproduct", "{\"drug_code\":42}")]
    [InlineData("activeingredient", "{\"drug_code\":42,\"ingredient_name\":\"EXAMPLE\",\"strength\":5,\"strength_unit\":\"MG\"}")]
    [InlineData("activeingredient", "{\"drug_code\":42,\"ingredient_name\":\"EXAMPLE\",\"strength\":\"5\",\"strength_unit\":\"MG\",\"dosage_value\":\"5\"}")]
    public async Task Rejects_missing_required_fields_and_invalid_types(string resource, string json)
    {
        var handler = new DpdStubHandler();
        handler.Responses[resource] = json;
        Assert.Equal(MedicationLookupOutcome.InvalidUpstreamData, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
    }

    [Theory]
    [InlineData(301)]
    [InlineData(404)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task Upstream_http_failure_is_not_unknown_product_or_partial_success(int status)
    {
        var handler = new DpdStubHandler { FailureResource = "route", StatusCode = (HttpStatusCode)status };
        var result = await Create(handler).LookupAsync("00001234", "en", default);
        Assert.Equal(MedicationLookupOutcome.UpstreamFailure, result.Outcome);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task Handles_network_failure_without_returning_exception_text()
    {
        var handler = new DpdStubHandler { Failure = new HttpRequestException("sensitive upstream detail") };
        Assert.Equal(MedicationLookupOutcome.UpstreamFailure, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
    }

    [Fact]
    public async Task Distinguishes_upstream_timeout_from_caller_cancellation()
    {
        var handler = new DpdStubHandler { Failure = new TaskCanceledException("timeout") };
        Assert.Equal(MedicationLookupOutcome.Timeout, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
        using var caller = new CancellationTokenSource();
        handler.OnRequest = () => caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(handler).LookupAsync("00001234", "en", caller.Token));
    }

    [Fact]
    public async Task Applies_deadline_to_pending_http_request()
    {
        var handler = new DpdStubHandler { WaitForCancellation = true };
        var result = await Create(handler, 1).LookupAsync("00001234", "en", default);
        Assert.Equal(MedicationLookupOutcome.Timeout, result.Outcome);
    }

    [Fact]
    public async Task Invalid_input_does_not_make_outbound_requests()
    {
        var handler = new DpdStubHandler();
        Assert.Equal(MedicationLookupOutcome.InvalidInput, (await Create(handler).LookupAsync("0000123", "en", default)).Outcome);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejects_oversized_response()
    {
        var handler = new DpdStubHandler();
        handler.Responses["drugproduct"] = new string(' ', 1_048_577);
        Assert.Equal(MedicationLookupOutcome.InvalidUpstreamData, (await Create(handler).LookupAsync("00001234", "en", default)).Outcome);
    }

    [Theory]
    [InlineData("http://health-products.canada.ca/api/drug/", 15)]
    [InlineData("https://example.test/api/drug/", 15)]
    [InlineData("https://health-products.canada.ca/api/drug/?redirect=evil", 15)]
    [InlineData("https://health-products.canada.ca/api/drug/", 0)]
    [InlineData("https://health-products.canada.ca/api/drug/", 31)]
    public void Rejects_unsafe_configuration(string url, int timeout) =>
        Assert.False(HealthCanadaDpdOptions.IsValid(new() { BaseUrl = url, TimeoutSeconds = timeout }));

    private static HealthCanadaDpdClient Create(DpdStubHandler handler, int timeout = 15) =>
        new(new HttpClient(handler), Options.Create(new HealthCanadaDpdOptions { TimeoutSeconds = timeout }), TimeProvider.System);
}
