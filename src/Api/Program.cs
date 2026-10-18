using MedicationCatalogue.Infrastructure;
using MedicationInformationPlatform.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddMedicationCatalogue(builder.Configuration);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.MapGet("/api/medications/{din}", MedicationEndpoint.LookupAsync);
app.Run();

public partial class Program { }
