using System.Net;
using ReleaseNotes.Web.Tests.Infrastructure;

namespace ReleaseNotes.Web.Tests;

[Collection(SiteCollection.Name)]
public sealed class HealthCheckTests(ReleaseNotesSite site)
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        HttpResponseMessage response = await site.CreateSiteClient().GetAsync("/healthz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
