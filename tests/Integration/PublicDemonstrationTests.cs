using System.Net;
using System.Text.Json;
using MedicationCatalogue.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MedicationApi.IntegrationTests;

public sealed class PublicDemonstrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public PublicDemonstrationTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Theory]
    [InlineData("en", "Aster", "Not for medication use")]
    [InlineData("fr", "Aster", "Ne pas utiliser")]
    [InlineData("ar", "أستر", "غير مُراجع")]
    public async Task Exact_public_reference_returns_only_explicit_fictional_fields(string language, string name, string disclaimer)
    {
        var response = await client.GetAsync($"/api/demo/references/{PublicDemonstrationCatalogue.Aster}?language={language}&patientId=private&actorId=private&organizationId=private");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(new[] { "contentStatus", "description", "disclaimer", "language", "name", "reference" }, root.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(PublicDemonstrationCatalogue.Aster, root.GetProperty("reference").GetString());
        Assert.Equal(language, root.GetProperty("language").GetString());
        Assert.Contains(name, root.GetProperty("name").GetString());
        Assert.Contains(disclaimer, root.GetProperty("disclaimer").GetString());
        Assert.Equal("fictional-unreviewed", root.GetProperty("contentStatus").GetString());
        Assert.DoesNotContain("private", root.ToString());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("00000000000000000000000000000000")]
    [InlineData("8A60F37D45BB4218A702DF2483E19B50")]
    [InlineData(PublicDemonstrationCatalogue.Withdrawn)]
    [InlineData(PublicDemonstrationCatalogue.Expired)]
    public async Task Unknown_invalid_withdrawn_and_expired_references_have_same_safe_response(string reference)
    {
        var response = await client.GetAsync("/api/demo/references/" + reference);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Demonstration unavailable", json.RootElement.GetProperty("title").GetString());
        Assert.False(json.RootElement.TryGetProperty("name", out _));
        Assert.DoesNotContain(reference, json.RootElement.ToString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("ar")]
    public async Task Catalogue_exposes_only_two_active_fictional_products(string language)
    {
        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/demo/products?language=" + language));
        Assert.Equal(new[] { PublicDemonstrationCatalogue.Aster, PublicDemonstrationCatalogue.Pebble },
            json.RootElement.EnumerateArray().Select(p => p.GetProperty("reference").GetString()));
        Assert.All(json.RootElement.EnumerateArray(), p => Assert.Equal(language, p.GetProperty("language").GetString()));
    }

    [Theory]
    [InlineData("/api/demo/products?language=de")]
    [InlineData("/api/demo/references/" + PublicDemonstrationCatalogue.Aster + "?language=de")]
    public async Task Unsupported_language_is_not_silently_substituted(string path) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);

    [Fact]
    public async Task Public_reference_is_not_a_DIN_or_write_capability()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/demo/references/00001234")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsync("/api/demo/products", null)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/api/demo/references/" + PublicDemonstrationCatalogue.Aster)).StatusCode);
    }
}
