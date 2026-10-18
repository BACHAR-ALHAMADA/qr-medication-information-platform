using System.Net;
using System.Text.Json;
using MedicationCatalogue.TestFixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MedicationApi.IntegrationTests;

public sealed class MedicationEndpointTests
{
    [Fact]
    public async Task Successful_lookup_exposes_only_source_metadata_and_no_approval()
    {
        var handler = new DpdStubHandler();
        // Unrecognized upstream fields, including simulated sensitive fields, are never passed through.
        handler.Responses["drugproduct"] = DpdStubHandler.Product.Replace("\"descriptor\":null",
            "\"patient_name\":\"SYNTHETIC-DO-NOT-RETURN\",\"isPublished\":true,\"descriptor\":null");
        using var factory = new TestApi(handler);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/medications/00001234");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SYNTHETIC-DO-NOT-RETURN", body);
        Assert.DoesNotContain("patient_name", body);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("00001234", root.GetProperty("din").GetString());
        Assert.Equal(42, root.GetProperty("dpdDrugCode").GetInt32());
        Assert.False(root.GetProperty("isPublished").GetBoolean());
        Assert.False(root.GetProperty("isApprovedPatientGuidance").GetBoolean());
        Assert.Equal("RegulatoryProductMetadata", root.GetProperty("contentClassification").GetString());
        Assert.Contains("Not patient-specific", root.GetProperty("notice").GetString());
        Assert.Equal("Health Canada Drug Product Database", root.GetProperty("source").GetProperty("provider").GetString());
    }

    [Theory]
    [InlineData("/api/medications/1234")]
    [InlineData("/api/medications/00001234?language=ar")]
    public async Task Bad_inputs_return_400_without_outbound_http(string url)
    {
        var handler = new DpdStubHandler();
        using var factory = new TestApi(handler);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("unknown", 404)]
    [InlineData("malformed", 502)]
    [InlineData("http", 502)]
    [InlineData("timeout", 504)]
    public async Task Errors_return_safe_problem_details(string scenario, int expected)
    {
        var handler = new DpdStubHandler();
        if (scenario == "unknown") handler.Responses["drugproduct"] = "[]";
        if (scenario == "malformed") handler.Responses["status"] = "{not-json";
        if (scenario == "http") handler.Failure = new HttpRequestException("secret-internal-details");
        if (scenario == "timeout") handler.Failure = new TaskCanceledException("secret-internal-details");
        using var factory = new TestApi(handler);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/medications/00001234");
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-internal-details", body);
        Assert.DoesNotContain("stackTrace", body);
        Assert.DoesNotContain("EXAMPLE PRODUCT", body);
    }

    [Fact]
    public async Task Caller_cannot_select_an_external_destination()
    {
        var handler = new DpdStubHandler();
        using var factory = new TestApi(handler);
        using var client = factory.CreateClient();
        await client.GetAsync("/api/medications/00001234?baseUrl=https%3A%2F%2Fexample.test%2F&language=fr");
        Assert.Equal(5, handler.Requests.Count);
        Assert.All(handler.Requests, uri => Assert.Equal("health-products.canada.ca", uri.Host));
    }

    private sealed class TestApi(DpdStubHandler handler) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureServices(services => services.AddHttpClient("IMedicationProductLookup")
                .ConfigurePrimaryHttpMessageHandler(() => handler));
    }
}
