# Release Notes CMS

A changelog site for software products, built on Umbraco 17 (LTS) and .NET 10. Editors manage products,
releases and release notes in the Umbraco backoffice; the public site lists them with filters, and a small
JSON API exposes the same data.

I built it as a technical demo of how I work with Umbraco: the content model is defined in code, editorial
rules run on save, rendering goes through typed controllers instead of logic in Razor, and the rules that
don't depend on the CMS live in a separate library with fast unit tests.

## Running it

Requires the .NET SDK 10.0.100 or later.

```bash
# The backoffice requires HTTPS
dotnet dev-certs https --trust

# First admin user for the unattended install (kept in user secrets, not in the repo)
dotnet user-secrets --project src/ReleaseNotes.Web set "Umbraco:CMS:Unattended:InstallUnattended" "true"
dotnet user-secrets --project src/ReleaseNotes.Web set "Umbraco:CMS:Unattended:UnattendedUserName" "Admin"
dotnet user-secrets --project src/ReleaseNotes.Web set "Umbraco:CMS:Unattended:UnattendedUserEmail" "admin@example.com"
dotnet user-secrets --project src/ReleaseNotes.Web set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<at least 10 characters>"

# Optional: without it, the installer generates a key and writes it into appsettings.json
dotnet user-secrets --project src/ReleaseNotes.Web set "Umbraco:CMS:Imaging:HMACSecretKey" "$(openssl rand -base64 64 | tr -d '\n')"

dotnet run --project src/ReleaseNotes.Web
```

On first start Umbraco installs into a SQLite file under `src/ReleaseNotes.Web/umbraco/Data`, the migration
creates the content model and, in Development, demo content is seeded. The site runs at
https://localhost:44346, the backoffice at `/umbraco` and Swagger at `/umbraco/swagger` (choose
"Release Notes API"); `/healthz` is the liveness probe. To start over, delete `umbraco/Data` and `wwwroot/media`.

With Docker:

```bash
cp .env.example .env    # fill in the admin credentials and HMAC key
docker compose up --build
```

This serves plain HTTP on http://localhost:8080 with `UseHttps=false`. Umbraco 17's backoffice login rejects
HTTP otherwise. That is fine for localhost, but a real deployment should terminate TLS in front of the container.

The demo content is three made-up products (ForgeKit, Lumen, Relay) with a set of releases covering every
change category, one release with breaking changes and one unpublished draft. The seeder only runs on an
empty content tree, once per database.

## How it's put together

```
src/
  ReleaseNotes.Core/     semver, ordering, filtering, paging, save-time version check. No Umbraco reference.
  ReleaseNotes.Web/      the Umbraco site
    ContentModel/        aliases and keys, migration, save handler, URL segments
    Catalog/             published content -> Core records
    Rendering/           page models and formatting; Controllers/ has the page, sitemap/robots and error controllers
    Api/                 /api/v1 endpoints, DTOs and mapping
    DemoContent/         seeder and the fictional catalog it creates
tests/
  ReleaseNotes.Core.Tests/   unit tests, run in well under a second
  ReleaseNotes.Web.Tests/    boot the real site against a throwaway SQLite database
```

The site and the API read from one place: `PublishedCatalogReader` walks the published content cache once
per request and projects it into immutable Core records. Drafts aren't in that cache, so they can't leak
into either.

### Content model

```
Home
├── Products (productCatalog)
│   └── Product
│       └── Release           version, date, type, summary, breaking flag, notes
│           └── notes: Block List of Release Note (title, category, description)
└── Changelog
```

Every document type also composes an "SEO Settings" type with an optional meta title and description.

## Decisions worth explaining

**Content model as a migration.** C# classes don't create document types in Umbraco, so something has to.
`CreateContentModel` is a `MigrationPlan` step that uses `IContentTypeService` and `IDataTypeService` with
fixed keys. Umbraco tracks the plan state, so the step runs once per database, and later schema changes
become new steps. I considered uSync, but it's a third-party dependency for a one-off schema.

**Demo content is not a migration.** Umbraco suppresses notifications while migrations run, and the URL
table, navigation and search indexes are all updated by notification handlers. Content created inside a
migration wouldn't be routable, so the seeder runs on `UmbracoApplicationStartedNotification` instead.

**Notes are blocks, not documents.** A note has no URL and no meaning outside its release. As Block List
items, editors add and reorder them inline, and block order is the display order, so there's no sort field.

**Hierarchy instead of pickers.** A release belongs to the product it's created under. That gives URLs
like `/products/forgekit/1-4-0/` for free, and unpublishing a product takes its releases with it. The release
URL comes from the version, not the node name; dots are replaced because a dotted last segment gets treated
as a static file.

**Save rules in a notification handler.** The version must be valid semver and unique within the product,
`v1.4.0 ` is stored as `1.4.0`, and a release type that doesn't match the version bump produces a warning.
Doing this in `ContentSavingNotification` means it also applies to the Management API and code, not just
the backoffice form.

**Route hijacking with typed page models.** One `RenderController` per document type builds a view model.
Document types have no templates, and ModelsBuilder is off. This keeps queries out of Razor. The changelog
filters are a plain GET form, so they work without JavaScript and every filtered view has a URL you can share.

**A small custom API next to the Delivery API.** The Content Delivery API is enabled, but it can't filter
releases by the category of their notes or sort by semantic version without custom index and sort handlers
(name sorting puts `10.0.0` before `9.0.0`). The three `/api/v1` endpoints cover that, with validation
shared with the changelog page and ProblemDetails for errors.

## API

| Endpoint | Returns |
| --- | --- |
| `GET /api/v1/releases` | Release summaries, newest first, paged |
| `GET /api/v1/products` | Products with their latest version and release count |
| `GET /api/v1/products/{slug}/releases/latest` | Latest stable release (or newest pre-release) with notes |

`/releases` accepts `product`, `category` (`feature`, `improvement`, `fix`, `security`, `breaking`),
`from` and `to` (`yyyy-MM-dd`, inclusive), `breaking=true`, `page` and `pageSize` (1–50, default 20).
Invalid parameters return 400 with one error per parameter.

```bash
curl "https://localhost:44346/api/v1/releases?product=lumen&category=security&from=2026-01-01"
```

## Tests

```bash
dotnet test
```

The Core tests cover semver parsing and precedence (including the ordering example from semver.org),
release ordering, filter validation, paging edge cases and the version check. The Web tests use
`WebApplicationFactory` to start the actual app, so the unattended install, migration and seeder all run.
They cover every page, the API contract, the sitemap, and editorial flows through `IContentService`:
publishing shows a release, unpublishing removes it, drafts don't leak, duplicate versions are rejected.

CI runs `dotnet format --verify-no-changes`, a Release build with warnings as errors, both test projects and
a Docker build with a smoke test.

## Limitations and what I'd do next

- SQLite is fine for a single instance. Anything shared should use SQL Server, which only needs a
  different connection string.
- Each request reads the whole catalog from the cache. That's fine at this size; a few thousand releases
  would call for an Examine index or output caching.
- Five SixLabors.ImageSharp advisories have no patched 3.x release, and Umbraco depends on 3.x. They're
  suppressed individually in `Directory.Build.props`, and TIFF uploads, which four of them concern, are
  disabled.
- On the very first start, a request in the first second can 404 while the demo content is being created.
- The site is English only.

Next steps I'd look at: RSS/Atom feeds, output caching invalidated on publish, and Delivery API index and
sort handlers so part of the custom API could go away.
