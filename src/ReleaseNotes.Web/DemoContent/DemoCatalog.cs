using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.DemoContent;

internal sealed record DemoProduct(
    string Name,
    string Tagline,
    string DescriptionHtml,
    string WebsiteUrl,
    string? RepositoryUrl,
    ProductLifecycle Lifecycle,
    bool IsFeatured,
    string LogoSvg,
    IReadOnlyList<DemoRelease> Releases);

internal sealed record DemoRelease(
    string Version,
    DateOnly Date,
    ReleaseType Type,
    string Summary,
    IReadOnlyList<DemoNote> Notes,
    bool Breaking = false,
    string? ExternalUrl = null,
    bool Publish = true);

internal sealed record DemoNote(ChangeCategory Category, string Title, string? DescriptionHtml = null);

// Fictional products; every link uses a reserved example.com domain.
internal static class DemoCatalog
{
    public const string HomeName = "Release Notes CMS";
    public const string MediaFolderName = "Demo content";
    public const string DemoNotice = "<p><em>Fictional product created as demo content for Release Notes CMS.</em></p>";

    public static IReadOnlyList<DemoProduct> Products { get; } =
    [
        new(
            "ForgeKit",
            "A fast, opinionated build toolkit for .NET monorepos.",
            "<p>ForgeKit replaces ad-hoc build scripts with a single workspace manifest, content-hashed incremental builds and a remote cache that teams can share.</p>" + DemoNotice,
            "https://forgekit.example.com",
            "https://git.example.com/forgekit/forgekit",
            ProductLifecycle.Active,
            IsFeatured: true,
            Logo("#4f46e5", "#818cf8", "M22 18h20v6H29v5h11v6H29v11h-7z"),
            [
                new("1.0.0", new DateOnly(2026, 1, 20), ReleaseType.Major, "First stable release: workspace manifests, incremental builds and a CLI you can script.",
                [
                    new(ChangeCategory.Feature, "Workspace manifest", "<p>Describe every project, target and dependency in a single <code>forge.toml</code> at the repository root.</p>"),
                    new(ChangeCategory.Feature, "Content-hashed incremental builds", "<p>Targets are skipped when the hash of their inputs is unchanged, even across branches.</p>"),
                    new(ChangeCategory.Feature, "Scriptable CLI with JSON output", "<p>Every command accepts <code>--json</code> for use in CI pipelines.</p>"),
                    new(ChangeCategory.Improvement, "Parallel package restore"),
                ]),
                new("1.1.0", new DateOnly(2026, 3, 3), ReleaseType.Minor, "Remote cache support and a quieter default output.",
                [
                    new(ChangeCategory.Feature, "Remote build cache", "<p>Share build outputs through any S3-compatible bucket. Configure it with <code>[cache.remote]</code> in the manifest.</p>"),
                    new(ChangeCategory.Improvement, "Condensed console output", "<p>Only failing targets are expanded by default. Use <code>--verbose</code> for the previous behaviour.</p>"),
                    new(ChangeCategory.Fix, "Restore no longer fails on paths containing spaces"),
                ]),
                new("1.2.0", new DateOnly(2026, 5, 12), ReleaseType.Minor, "Watch mode, test sharding and a hardened cache client.",
                [
                    new(ChangeCategory.Security, "Cache downloads are verified against their content hash", "<p>Artifacts fetched from the remote cache are now rejected when their hash does not match, preventing cache poisoning.</p>"),
                    new(ChangeCategory.Feature, "Watch mode", "<p><code>forge watch</code> rebuilds affected targets as files change.</p>"),
                    new(ChangeCategory.Feature, "Test sharding across CI agents"),
                    new(ChangeCategory.Fix, "Correct exit code when a target is cancelled"),
                ]),
                new("1.3.0", new DateOnly(2026, 7, 8), ReleaseType.Minor, "Dependency graph visualisation and faster cold starts.",
                [
                    new(ChangeCategory.Feature, "Interactive dependency graph", "<p><code>forge graph --open</code> renders the workspace graph in the browser.</p>"),
                    new(ChangeCategory.Improvement, "Cold start time reduced by 40%"),
                    new(ChangeCategory.Improvement, "Clearer error when a target name is ambiguous"),
                ]),
                new("1.3.1", new DateOnly(2026, 8, 2), ReleaseType.Patch, "Patch release fixing cache invalidation edge cases.",
                [
                    new(ChangeCategory.Fix, "Environment variables are part of the target hash", "<p>Targets that read environment variables declared in <code>inputs.env</code> are now rebuilt when those values change.</p>"),
                    new(ChangeCategory.Fix, "Watch mode ignores editor swap files"),
                    new(ChangeCategory.Security, "Remote cache credentials are redacted from logs"),
                ]),
                new("1.4.0", new DateOnly(2026, 10, 24), ReleaseType.Minor, "Plugins, workspace templates and faster remote cache uploads.",
                [
                    new(ChangeCategory.Feature, "Plugin API", "<p>Extend ForgeKit with custom target types written in C#. Plugins are loaded from the <code>plugins</code> section of the manifest.</p>"),
                    new(ChangeCategory.Feature, "Workspace templates", "<p><code>forge new</code> scaffolds a workspace from a template repository.</p>"),
                    new(ChangeCategory.Improvement, "Remote cache uploads are streamed", "<p>Large artifacts no longer need to fit in memory before upload.</p>"),
                    new(ChangeCategory.Improvement, "Faster graph resolution for workspaces with more than 500 projects"),
                    new(ChangeCategory.Fix, "forge clean removes empty output directories"),
                    new(ChangeCategory.Fix, "Correct relative paths in generated launch profiles on Windows"),
                ],
                ExternalUrl: "https://forgekit.example.com/blog/1-4"),
                new("1.5.0-beta.1", new DateOnly(2026, 11, 16), ReleaseType.Minor, "Draft release that is not published yet.",
                [
                    new(ChangeCategory.Feature, "Distributed task execution (preview)"),
                ],
                Publish: false),
            ]),
        new(
            "Lumen",
            "Structured logging and tracing SDK for distributed services.",
            "<p>Lumen gives every service the same structured log format and propagates trace context across HTTP, gRPC and message queues with a single line of setup.</p>" + DemoNotice,
            "https://lumen.example.com",
            "https://git.example.com/lumen/lumen-dotnet",
            ProductLifecycle.Active,
            IsFeatured: true,
            Logo("#0891b2", "#22d3ee", "M23 17h7v23h13v6H23z"),
            [
                new("1.0.0", new DateOnly(2025, 11, 4), ReleaseType.Major, "First stable release of the Lumen SDK.",
                [
                    new(ChangeCategory.Feature, "Structured logging with source-generated message templates"),
                    new(ChangeCategory.Feature, "W3C trace context propagation for HTTP clients and servers"),
                    new(ChangeCategory.Feature, "Console and OTLP exporters"),
                ]),
                new("1.1.0", new DateOnly(2026, 1, 14), ReleaseType.Minor, "Message queue propagation and sampling rules.",
                [
                    new(ChangeCategory.Feature, "Trace propagation for message queues", "<p>Built-in propagators for RabbitMQ and Azure Service Bus.</p>"),
                    new(ChangeCategory.Feature, "Rule-based sampling"),
                    new(ChangeCategory.Fix, "Scopes are no longer lost across await boundaries in custom schedulers"),
                ]),
                new("1.2.0", new DateOnly(2026, 4, 9), ReleaseType.Minor, "Redaction of sensitive fields and lower allocation rates.",
                [
                    new(ChangeCategory.Security, "Automatic redaction of sensitive fields", "<p>Fields named like credentials (<code>password</code>, <code>token</code>, <code>secret</code>) are redacted before export.</p>"),
                    new(ChangeCategory.Improvement, "60% fewer allocations per log event"),
                    new(ChangeCategory.Fix, "OTLP exporter retries on HTTP 503"),
                ]),
                new("2.0.0", new DateOnly(2026, 6, 30), ReleaseType.Major, "A leaner configuration API and OpenTelemetry-native metrics. Read the migration notes before upgrading.",
                [
                    new(ChangeCategory.Breaking, "Configuration moved to a single builder", "<p><code>LumenOptions</code> was replaced by <code>services.AddLumen(builder =&gt; ...)</code>. The old options class is removed.</p><p>Migration: move each option to the matching builder method; names are unchanged.</p>"),
                    new(ChangeCategory.Breaking, ".NET 8 is no longer supported", "<p>Lumen 2 targets .NET 10. Stay on 1.x for .NET 8 applications.</p>"),
                    new(ChangeCategory.Feature, "OpenTelemetry metrics", "<p>Counters and histograms are exported through the same pipeline as traces.</p>"),
                    new(ChangeCategory.Improvement, "Exporter batches are flushed on shutdown"),
                ],
                Breaking: true,
                ExternalUrl: "https://lumen.example.com/docs/migrate-to-2"),
                new("2.0.1", new DateOnly(2026, 7, 21), ReleaseType.Patch, "Fixes for the new builder API.",
                [
                    new(ChangeCategory.Fix, "AddLumen can be called more than once without duplicating exporters"),
                    new(ChangeCategory.Fix, "Metric names keep their unit suffix"),
                ]),
                new("2.1.0", new DateOnly(2026, 9, 29), ReleaseType.Minor, "Log-to-trace correlation in the console exporter and a security fix.",
                [
                    new(ChangeCategory.Security, "Header values are no longer captured by default", "<p>Request and response headers are excluded unless explicitly allowed with <code>CaptureHeaders()</code>.</p>"),
                    new(ChangeCategory.Feature, "Trace identifiers in console output"),
                    new(ChangeCategory.Improvement, "Smaller package size"),
                ]),
            ]),
        new(
            "Relay",
            "Webhook delivery with retries, signing and a replay console.",
            "<p>Relay accepts events from your application and delivers them to subscriber endpoints with signed payloads, exponential back-off and a console to inspect and replay deliveries.</p>" + DemoNotice,
            "https://relay.example.com",
            null,
            ProductLifecycle.Beta,
            IsFeatured: false,
            Logo("#db2777", "#f472b6", "M23 17h12a8 8 0 0 1 2 15.7L44 46h-8l-6-12h-1v12h-6zm6 6v6h5a3 3 0 0 0 0-6z"),
            [
                new("0.1.0", new DateOnly(2026, 8, 18), ReleaseType.Minor, "Public beta: signed deliveries with automatic retries.",
                [
                    new(ChangeCategory.Feature, "HMAC-SHA256 signed payloads"),
                    new(ChangeCategory.Feature, "Exponential back-off with jitter"),
                    new(ChangeCategory.Feature, "Delivery log with response bodies"),
                ]),
                new("0.2.0", new DateOnly(2026, 10, 6), ReleaseType.Minor, "Replay console and endpoint health tracking.",
                [
                    new(ChangeCategory.Feature, "Replay console", "<p>Select failed deliveries and send them again after fixing the subscriber.</p>"),
                    new(ChangeCategory.Feature, "Endpoints are paused after repeated failures"),
                    new(ChangeCategory.Fix, "Retries respect the Retry-After header"),
                ]),
            ]),
    ];

    private static string Logo(string from, string to, string glyphPath) =>
        $"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">
          <defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{from}"/><stop offset="1" stop-color="{to}"/></linearGradient></defs>
          <rect width="64" height="64" rx="16" fill="url(#g)"/>
          <path d="{glyphPath}" fill="#fff"/>
        </svg>
        """;
}
