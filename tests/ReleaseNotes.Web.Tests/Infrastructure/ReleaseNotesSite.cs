using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using IHostingEnvironment = Umbraco.Cms.Core.Hosting.IHostingEnvironment;

namespace ReleaseNotes.Web.Tests.Infrastructure;

// Boots the real site against a new SQLite database: unattended install, migration and demo seeding
// all run. Shared by all integration tests because startup takes several seconds.
public sealed class ReleaseNotesSite : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "releasenotes-tests", Guid.NewGuid().ToString("N"));
    private string? _umbracoTempPath;

    public ReleaseNotesSite()
    {
        Directory.CreateDirectory(_root);

        // Umbraco reads part of its configuration while Program builds the host, before
        // WebApplicationFactory configuration callbacks run, so settings go in as environment variables.
        Dictionary<string, string> settings = new()
        {
            ["ConnectionStrings__umbracoDbDSN"] = $"Data Source={Path.Combine(_root, "Umbraco.sqlite.db")};Cache=Shared;Foreign Keys=True;Pooling=True",
            ["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
            ["Umbraco__CMS__Unattended__InstallUnattended"] = "true",
            ["Umbraco__CMS__Unattended__UnattendedUserName"] = "Integration Tests",
            ["Umbraco__CMS__Unattended__UnattendedUserEmail"] = "tests@example.com",
            ["Umbraco__CMS__Unattended__UnattendedUserPassword"] = $"Tst-{Guid.NewGuid():N}",

            // With a key configured, the installer does not generate one and write it into appsettings.json.
            ["Umbraco__CMS__Imaging__HMACSecretKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ["Umbraco__CMS__Hosting__SiteName"] = $"releasenotes-tests-{Path.GetFileName(_root)}",
            ["Umbraco__CMS__Hosting__LocalTempStorageLocation"] = "EnvironmentTemp",
            ["Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath"] = Path.Combine(_root, "media"),
            ["Umbraco__CMS__Logging__Directory"] = Path.Combine(_root, "logs"),
            ["ReleaseNotes__DemoContent__Enabled"] = "true",
            ["Serilog__MinimumLevel__Default"] = "Warning",
        };

        foreach ((var key, var value) in settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public ValueTask InitializeAsync()
    {
        // Creating a client starts the host: install, migrations and seeding complete before it returns.
        using HttpClient client = CreateClient();
        _umbracoTempPath = Services.GetRequiredService<IHostingEnvironment>().LocalTempPath;
        return ValueTask.CompletedTask;
    }

    public HttpClient CreateSiteClient() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        TryDelete(_root);
        if (_umbracoTempPath is not null)
        {
            TryDelete(_umbracoTempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // SQLite or Lucene may still hold a handle for a moment; the OS temp folder is cleaned eventually.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

[CollectionDefinition(Name)]
public sealed class SiteCollection : ICollectionFixture<ReleaseNotesSite>
{
    public const string Name = "Release Notes site";
}
