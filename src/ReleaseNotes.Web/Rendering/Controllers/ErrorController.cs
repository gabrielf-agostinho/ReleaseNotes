using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Web.Catalog;

namespace ReleaseNotes.Web.Rendering.Controllers;

// Exception handler target outside Development: ProblemDetails for /api/, the error page otherwise.
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : Controller
{
    public const string Path = "/error";

    private readonly PublishedCatalogReader _catalogReader;
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(PublishedCatalogReader catalogReader, ILogger<ErrorController> logger)
    {
        _catalogReader = catalogReader;
        _logger = logger;
    }

    [Route(Path)]
    public IActionResult Index()
    {
        var originalPath = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? string.Empty;
        if (originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpected error occurred.");
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        var meta = new PageMeta("Something went wrong", "An unexpected error occurred.", Path, NoIndex: true);
        return View("~/Views/Pages/Error.cshtml", new ErrorPageModel(meta, SafeLinks()));
    }

    // The exception may have come from the content cache, so don't let the error page depend on it.
    private SiteLinks SafeLinks()
    {
        try
        {
            return _catalogReader.GetSnapshot().Links;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not read site links for the error page");
            return CatalogSnapshot.Empty.Links;
        }
    }
}
