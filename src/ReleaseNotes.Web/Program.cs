using ReleaseNotes.Web.Rendering.Controllers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
#endif

// Our own registrations are in ReleaseNotesComposer.
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(ErrorController.Path);
}

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
        u.EndpointRouteBuilder.MapHealthChecks("/healthz");
    });

await app.RunAsync();

// For WebApplicationFactory in the integration tests.
public partial class Program;
