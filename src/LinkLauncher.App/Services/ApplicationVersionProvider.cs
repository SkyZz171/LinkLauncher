namespace LinkLauncher.App.Services;

using System.Reflection;
using LinkLauncher.Core.Abstractions;

/// <summary>Lit la version informative de l'assembly de l'application.</summary>
public sealed class ApplicationVersionProvider : IApplicationVersionProvider
{
    /// <summary>Crée le fournisseur de version.</summary>
    public ApplicationVersionProvider()
    {
        var assembly = typeof(ApplicationVersionProvider).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = assembly.GetName().Version;

        Version = informationalVersion?.Split('+')[0] ?? version?.ToString(3) ?? "0.0.0";
    }

    /// <inheritdoc />
    public string Version { get; }
}
