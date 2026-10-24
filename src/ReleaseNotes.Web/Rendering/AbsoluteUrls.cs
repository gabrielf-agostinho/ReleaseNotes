namespace ReleaseNotes.Web.Rendering;

public static class AbsoluteUrls
{
    public static string ToAbsolute(this HttpRequest request, string url) =>
        IsHttpUrl(url) ? url : $"{request.Scheme}://{request.Host}{request.PathBase}{url}";

    // Checking UriKind.Absolute alone is not enough: on Unix "/products/" parses as a file:// URI.
    public static bool IsHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
