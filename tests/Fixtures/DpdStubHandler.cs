using System.Net;
using System.Text;

namespace MedicationCatalogue.TestFixtures;

// Fictional public product fixtures; no patient records or live HTTP calls.
public sealed class DpdStubHandler : HttpMessageHandler
{
    public const string Product = """
        {"drug_code":42,"drug_identification_number":"00001234","brand_name":"EXAMPLE PRODUCT","class_name":"Human","company_name":"EXAMPLE COMPANY","number_of_ais":"1","descriptor":null,"last_update_date":null}
        """;
    public Dictionary<string, string> Responses { get; } = new()
    {
        ["drugproduct"] = Product,
        ["activeingredient"] = """[{"drug_code":42,"ingredient_name":"EXAMPLE INGREDIENT","strength":"5","strength_unit":"MG","dosage_value":"","dosage_unit":null}]""",
        ["form"] = """{"drug_code":42,"pharmaceutical_form_name":"Tablet"}""",
        ["route"] = """[{"drug_code":42,"route_of_administration_name":"Oral"}]""",
        ["status"] = """{"drug_code":42,"status":"Marketed","external_status_code":2,"history_date":"2026-01-01","original_market_date":null}"""
    };
    public List<Uri> Requests { get; } = [];
    public string? FailureResource { get; set; }
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public Exception? Failure { get; set; }
    public Action? OnRequest { get; set; }
    public bool WaitForCancellation { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        OnRequest?.Invoke();
        if (WaitForCancellation)
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null)
            throw Failure;
        var resource = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return new HttpResponseMessage(resource == FailureResource ? StatusCode : HttpStatusCode.OK)
        {
            Content = new StringContent(Responses[resource], Encoding.UTF8, "application/json")
        };
    }
}
