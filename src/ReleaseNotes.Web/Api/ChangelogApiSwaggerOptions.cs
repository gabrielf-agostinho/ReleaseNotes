using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ReleaseNotes.Web.Api;

// Shows up as its own document in Umbraco's Swagger UI (/umbraco/swagger), which is off in Production.
public sealed class ChangelogApiSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(ChangelogApiController.ApiName, new()
        {
            Title = "Release Notes API",
            Version = "v1",
            Description = "Read-only queries over published products and releases. Only published content is returned.",
        });

        var xmlDocs = Path.Combine(AppContext.BaseDirectory, $"{typeof(ChangelogApiController).Assembly.GetName().Name}.xml");
        if (File.Exists(xmlDocs))
        {
            options.IncludeXmlComments(xmlDocs);
        }
    }
}
