namespace ReleaseNotes.Web.ContentModel;

// Keys are fixed so the migration is idempotent and every environment ends up with the same model.
public static class ContentTypes
{
    public static class SeoSettings
    {
        public const string Alias = "seoSettings";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1001");

        public const string MetaTitle = "metaTitle";
        public const string MetaDescription = "metaDescription";
    }

    public static class Home
    {
        public const string Alias = "home";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1002");

        public const string HeroTitle = "heroTitle";
        public const string HeroText = "heroText";
    }

    public static class ProductCatalog
    {
        public const string Alias = "productCatalog";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1003");

        public const string Intro = "intro";
    }

    public static class Product
    {
        public const string Alias = "product";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1004");

        public const string Tagline = "tagline";
        public const string Description = "description";
        public const string Logo = "logo";
        public const string WebsiteUrl = "websiteUrl";
        public const string RepositoryUrl = "repositoryUrl";
        public const string Lifecycle = "lifecycle";
        public const string IsFeatured = "isFeatured";

        // Umbraco convention: a property with this alias overrides the URL segment.
        public const string Slug = "umbracoUrlName";
    }

    public static class Release
    {
        public const string Alias = "release";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1005");

        public const string Version = "version";
        public const string ReleaseDate = "releaseDate";
        public const string ReleaseType = "releaseType";
        public const string Summary = "summary";
        public const string HasBreakingChanges = "hasBreakingChanges";
        public const string Notes = "notes";
        public const string ExternalUrl = "externalUrl";
    }

    public static class Changelog
    {
        public const string Alias = "changelog";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1006");

        public const string Intro = "intro";
    }

    // Element type for the blocks in Release.Notes.
    public static class ReleaseNote
    {
        public const string Alias = "releaseNote";
        public static readonly Guid Key = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c1007");

        public const string Title = "title";
        public const string Category = "category";
        public const string Description = "description";
    }
}

public static class DataTypeKeys
{
    public static readonly Guid ReleaseDate = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2001");
    public static readonly Guid ReleaseType = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2002");
    public static readonly Guid ChangeCategory = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2003");
    public static readonly Guid ProductLifecycle = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2004");
    public static readonly Guid ReleaseNotes = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2005");
    public static readonly Guid ProductLogo = new("6f2e0c3a-1c9e-4a61-9a37-2f8e4f0c2006");
}
