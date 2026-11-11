namespace LinkLauncher.Features.Settings;

using LinkLauncher.Core.Models;

/// <summary>Couleur d'accentuation sélectionnable dans les paramètres.</summary>
/// <param name="Key">Couleur représentée.</param>
/// <param name="Label">Libellé affiché.</param>
public sealed record AccentOption(AccentColorKey Key, string Label);
