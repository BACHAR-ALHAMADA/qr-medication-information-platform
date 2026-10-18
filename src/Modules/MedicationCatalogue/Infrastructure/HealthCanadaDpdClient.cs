using System.Globalization;
using System.Text.Json;
using MedicationCatalogue.Application;
using Microsoft.Extensions.Options;

namespace MedicationCatalogue.Infrastructure;

public sealed class HealthCanadaDpdClient : IMedicationProductLookup
{
    private const int MaximumResponseBytes = 1_048_576;
    private readonly HttpClient httpClient;
    private readonly HealthCanadaDpdOptions options;
    private readonly TimeProvider timeProvider;

    public HealthCanadaDpdClient(HttpClient httpClient, IOptions<HealthCanadaDpdOptions> options, TimeProvider timeProvider)
    {
        if (!HealthCanadaDpdOptions.IsValid(options.Value))
            throw new ArgumentException("Invalid DPD configuration.", nameof(options));
        this.httpClient = httpClient;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<MedicationLookupResult> LookupAsync(string din, string language, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!MedicationLookupInput.IsValid(din, language))
            return new(MedicationLookupOutcome.InvalidInput);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            var products = await ReadRowsAsync($"drugproduct/?din={din}&lang={language}&type=json", deadline.Token);
            if (products.Count == 0)
                return new(MedicationLookupOutcome.NotFound);
            if (products.Count != 1)
                throw new InvalidDataException();
            var product = products[0];
            if (RequiredText(product, "drug_identification_number") != din)
                throw new InvalidDataException();
            var code = RequiredPositiveInt(product, "drug_code");
            var brand = RequiredText(product, "brand_name");
            var company = RequiredText(product, "company_name");
            var productClass = RequiredText(product, "class_name");
            var ingredientCount = RequiredPositiveInt(product, "number_of_ais");

            // Sequential requests keep a lookup to one in-flight upstream request at a time.
            var suffix = $"?id={code.ToString(CultureInfo.InvariantCulture)}&lang={language}&type=json";
            var ingredients = await ReadRowsAsync("activeingredient/" + suffix, deadline.Token);
            var forms = await ReadRowsAsync("form/" + suffix, deadline.Token);
            var routes = await ReadRowsAsync("route/" + suffix, deadline.Token);
            var statuses = await ReadRowsAsync("status/" + suffix, deadline.Token);
            if (ingredients.Count != ingredientCount || forms.Count == 0 || routes.Count == 0 || statuses.Count != 1)
                throw new InvalidDataException();
            foreach (var row in ingredients.Concat(forms).Concat(routes).Concat(statuses))
                if (RequiredPositiveInt(row, "drug_code") != code)
                    throw new InvalidDataException();

            var normalizedIngredients = ingredients.Select(row =>
            {
                var dosageValue = OptionalText(row, "dosage_value");
                var dosageUnit = OptionalText(row, "dosage_unit");
                if ((dosageValue is null) != (dosageUnit is null))
                    throw new InvalidDataException();
                return new MedicationIngredient(RequiredText(row, "ingredient_name"), RequiredText(row, "strength"),
                    RequiredText(row, "strength_unit"), dosageValue, dosageUnit);
            }).ToArray();
            var status = statuses[0];
            var metadata = new MedicationProductMetadata(din, code, brand, company, productClass,
                OptionalText(product, "descriptor"), OptionalDate(product, "last_update_date"), normalizedIngredients,
                forms.Select(row => RequiredText(row, "pharmaceutical_form_name")).ToArray(),
                routes.Select(row => RequiredText(row, "route_of_administration_name")).ToArray(),
                new MedicationProductStatus(RequiredText(status, "status"), RequiredPositiveInt(status, "external_status_code"),
                    OptionalDate(status, "history_date"), OptionalDate(status, "original_market_date")),
                new MedicationSourceProvenance("Health Canada Drug Product Database", new Uri(options.BaseUrl),
                    new Uri($"https://health-products.canada.ca/dpd-bdpp/info?code={code.ToString(CultureInfo.InvariantCulture)}&lang={(language == "fr" ? "fre" : "eng")}"),
                    language, timeProvider.GetUtcNow()));
            return new(MedicationLookupOutcome.Found, metadata);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(MedicationLookupOutcome.Timeout);
        }
        catch (HttpRequestException)
        {
            return new(MedicationLookupOutcome.UpstreamFailure);
        }
        catch (InvalidDataException)
        {
            return new(MedicationLookupOutcome.InvalidUpstreamData);
        }
        catch (IOException)
        {
            return new(MedicationLookupOutcome.InvalidUpstreamData);
        }
        catch (JsonException)
        {
            return new(MedicationLookupOutcome.InvalidUpstreamData);
        }
    }

    private async Task<IReadOnlyList<JsonElement>> ReadRowsAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        // Compose only fixed paths and validated DIN/language/internal numeric codes.
        using var response = await httpClient.GetAsync(new Uri(new Uri(options.BaseUrl), relativeUrl),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("DPD request failed.");
        if (response.Content.Headers.ContentType?.MediaType is not "application/json")
            throw new InvalidDataException();
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            throw new InvalidDataException();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            if (body.Length + count > MaximumResponseBytes)
                throw new InvalidDataException();
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        var rows = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().Select(row => row.Clone()).ToArray(),
            JsonValueKind.Object => new[] { root.Clone() },
            _ => throw new InvalidDataException()
        };
        if (rows.Any(row => row.ValueKind != JsonValueKind.Object))
            throw new InvalidDataException();
        return rows;
    }

    private static string RequiredText(JsonElement row, string name) => OptionalText(row, name) ?? throw new InvalidDataException();

    private static string? OptionalText(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException();
        var text = value.GetString();
        if (text?.Length > 1000)
            throw new InvalidDataException();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int RequiredPositiveInt(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value))
            throw new InvalidDataException();
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number : throw new InvalidDataException();
    }

    private static DateOnly? OptionalDate(JsonElement row, string name)
    {
        var text = OptionalText(row, name);
        if (text is null)
            return null;
        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date : throw new InvalidDataException();
    }
}
