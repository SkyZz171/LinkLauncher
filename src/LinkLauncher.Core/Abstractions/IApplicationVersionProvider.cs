namespace LinkLauncher.Core.Abstractions;

/// <summary>Version de l'application affichée dans l'interface.</summary>
public interface IApplicationVersionProvider
{
    /// <summary>Version lisible de LinkLauncher.</summary>
    string Version { get; }
}
