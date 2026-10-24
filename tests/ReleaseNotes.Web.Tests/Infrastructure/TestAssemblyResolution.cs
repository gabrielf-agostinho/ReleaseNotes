using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace ReleaseNotes.Web.Tests.Infrastructure;

internal static class TestAssemblyResolution
{
    // Umbraco's type scanner loads every assembly next to the entry assembly, which here is the xunit
    // runner. It references xunit.abstractions, which is in the output folder but not in deps.json,
    // so the default load context can't find it by name.
    [ModuleInitializer]
    internal static void Register() =>
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, $"{name.Name}.dll");
            return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
        };
}
