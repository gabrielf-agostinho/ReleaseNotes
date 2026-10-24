using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Web.Catalog;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;
using Seo = ReleaseNotes.Web.ContentModel.ContentTypes.SeoSettings;

namespace ReleaseNotes.Web.Rendering.Controllers;

// Umbraco route hijacking: each document type is routed to the controller named after its alias
// (ReleaseController for "release"), so document types need no templates.
public abstract class SitePageController : RenderController
{
    private readonly IPublishedValueFallback _valueFallback;

    protected SitePageController(SitePageDependencies dependencies)
        : base(dependencies.Logger, dependencies.ViewEngine, dependencies.UmbracoContextAccessor)
    {
        CatalogReader = dependencies.CatalogReader;
        _valueFallback = dependencies.ValueFallback;
    }

    protected PublishedCatalogReader CatalogReader { get; }

    protected CatalogSnapshot Catalog => CatalogReader.GetSnapshot();

    protected IPublishedContent Page => CurrentPage ?? throw new InvalidOperationException("No published content is routed to this controller.");

    protected string? Text(string alias)
    {
        var value = Page.Value<string>(_valueFallback, alias);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // The SEO composition fields win over the defaults passed in.
    protected PageMeta Meta(string title, string? description, string canonicalPath, string openGraphType = "website", string? imageUrl = null, bool noIndex = false) =>
        new(
            Text(Seo.MetaTitle) ?? title,
            Text(Seo.MetaDescription) ?? description ?? string.Empty,
            canonicalPath,
            openGraphType,
            imageUrl,
            noIndex);

    protected IActionResult PageView(string name, SitePageModel model, int statusCode = StatusCodes.Status200OK)
    {
        Response.StatusCode = statusCode;
        return View($"~/Views/Pages/{name}.cshtml", model);
    }

    protected IActionResult NotFoundPage() =>
        PageView("NotFound", NotFoundPageModel.Create(Catalog.Links), StatusCodes.Status404NotFound);

    // Anything that is not a positive integer means page 1.
    protected int RequestedPage() =>
        int.TryParse(Request.Query["page"], NumberStyles.None, CultureInfo.InvariantCulture, out var page) && page > 0
            ? page
            : 1;

    protected static string WithPage(string path, int page) => page > 1 ? $"{path}?page={page}" : path;
}

// Keeps the derived controllers' constructors to a single parameter.
public sealed record SitePageDependencies(
    ILogger<RenderController> Logger,
    ICompositeViewEngine ViewEngine,
    IUmbracoContextAccessor UmbracoContextAccessor,
    PublishedCatalogReader CatalogReader,
    IPublishedValueFallback ValueFallback);
