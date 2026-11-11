namespace LinkLauncher.Features.Library;

/// <summary>Profil de lancement tel qu'affiché dans la liste.</summary>
/// <param name="Id">Identifiant du profil.</param>
/// <param name="Label">Nom affiché.</param>
/// <param name="Description">Langue appliquée, ou mention de la langue du système.</param>
public sealed record ProfileOption(Guid Id, string Label, string Description);
