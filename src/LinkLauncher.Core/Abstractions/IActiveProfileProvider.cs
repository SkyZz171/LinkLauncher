namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Fournit le profil de lancement actif aux services qui en dépendent.</summary>
public interface IActiveProfileProvider
{
    /// <summary>Profil actif, ou <see langword="null"/> si la bibliothèque n'en contient aucun.</summary>
    LaunchProfile? GetActiveProfile();
}
