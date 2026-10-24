using Microsoft.AspNetCore.Http;
using ReleaseNotes.Web.Rendering;

namespace ReleaseNotes.Web.Tests;

public sealed class AbsoluteUrlsTests
{
    [Theory]
    [InlineData("/products/forgekit/", "https://changelog.example.com/products/forgekit/")]
    [InlineData("/media/abc/logo.svg", "https://changelog.example.com/media/abc/logo.svg")]
    [InlineData("https://cdn.example.com/logo.svg", "https://cdn.example.com/logo.svg")]
    [InlineData("http://other.example.com/", "http://other.example.com/")]
    public void Root_relative_urls_get_the_request_origin(string url, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("changelog.example.com");

        Assert.Equal(expected, context.Request.ToAbsolute(url));
    }

    [Fact]
    public void A_root_relative_path_is_not_an_http_url_even_where_it_parses_as_a_file_uri()
    {
        // On Linux and macOS, Uri.TryCreate("/products/", UriKind.Absolute, ...) succeeds with a file:// URI.
        Assert.False(AbsoluteUrls.IsHttpUrl("/products/"));
        Assert.True(AbsoluteUrls.IsHttpUrl("https://example.com/products/"));
    }
}
