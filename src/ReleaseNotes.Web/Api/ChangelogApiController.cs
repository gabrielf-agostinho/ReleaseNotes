using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Web.Catalog;
using ReleaseNotes.Web.Rendering;
using Umbraco.Cms.Api.Common.Attributes;

namespace ReleaseNotes.Web.Api;

// Covers what the Delivery API can't do without custom index handlers: filtering by note category and
// sorting by date/semver. Read-only; content is edited in the backoffice.
[ApiController]
[Route("api/v1")]
[MapToApi(ApiName)]
public sealed class ChangelogApiController : ControllerBase
{
    public const string ApiName = "release-notes";

    private const int DefaultPageSize = 20;

    private readonly PublishedCatalogReader _catalogReader;

    public ChangelogApiController(PublishedCatalogReader catalogReader) => _catalogReader = catalogReader;

    /// <summary>Lists published releases, newest first, with optional combined filters.</summary>
    /// <param name="product">Product slug, e.g. forgekit.</param>
    /// <param name="category">Only releases containing a note of this category: feature, improvement, fix, security or breaking.</param>
    /// <param name="from">Earliest release date (inclusive), yyyy-MM-dd.</param>
    /// <param name="to">Latest release date (inclusive), yyyy-MM-dd.</param>
    /// <param name="breaking">true to return only releases with breaking changes.</param>
    /// <param name="page">1-based page number. Default 1.</param>
    /// <param name="pageSize">Items per page, 1 to 50. Default 20.</param>
    [HttpGet("releases")]
    [ProducesResponseType<PagedResponse<ReleaseSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public ActionResult<PagedResponse<ReleaseSummaryDto>> GetReleases(
        [FromQuery] string? product,
        [FromQuery] string? category,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? breaking,
        [FromQuery] string? page,
        [FromQuery] string? pageSize)
    {
        CatalogSnapshot catalog = _catalogReader.GetSnapshot();
        ReleaseFilterResult result = ReleaseFilterResult.Parse(
            new ReleaseQueryParameters(product, category, from, to, breaking, page, pageSize),
            catalog.ProductSlugs,
            DefaultPageSize);

        if (!result.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach ((var parameter, var message) in result.Errors)
            {
                modelState.AddModelError(parameter, message);
            }

            return ValidationProblem(title: "One or more query parameters are invalid.", modelStateDictionary: modelState);
        }

        return result.Filter.Apply(catalog.Releases).ToResponse(release => release.ToSummaryDto(Absolute));
    }

    /// <summary>Lists published products with their latest release.</summary>
    [HttpGet("products")]
    [ProducesResponseType<IReadOnlyList<ProductSummaryDto>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ProductSummaryDto>> GetProducts()
    {
        CatalogSnapshot catalog = _catalogReader.GetSnapshot();
        return catalog.Products.Select(p => p.ToSummaryDto(catalog, Absolute)).ToArray();
    }

    /// <summary>Gets the latest release of a product, including all notes.</summary>
    /// <param name="slug">Product slug, e.g. forgekit.</param>
    [HttpGet("products/{slug}/releases/latest")]
    [ProducesResponseType<ReleaseDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<ReleaseDetailDto> GetLatestRelease(string slug)
    {
        CatalogSnapshot catalog = _catalogReader.GetSnapshot();
        Product? product = catalog.FindProductBySlug(slug);
        if (product is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Product '{slug}' was not found.");
        }

        Release? latest = catalog.LatestReleaseOf(product.Key);
        return latest is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: $"Product '{slug}' has no published releases yet.")
            : latest.ToDetailDto(Absolute);
    }

    private string Absolute(string url) => Request.ToAbsolute(url);
}
