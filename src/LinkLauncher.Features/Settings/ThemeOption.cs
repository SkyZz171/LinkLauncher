namespace LinkLauncher.Features.Settings;

using LinkLauncher.Core.Models;

/// <summary>Thème sélectionnable dans les paramètres.</summary>
/// <param name="Value">Thème représenté.</param>
/// <param name="Label">Libellé affiché.</param>
/// <param name="Description">Explication courte.</param>
public sealed record ThemeOption(AppTheme Value, string Label, string Description);
