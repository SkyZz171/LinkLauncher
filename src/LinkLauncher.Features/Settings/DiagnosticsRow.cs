namespace LinkLauncher.Features.Settings;

/// <summary>Ligne du rapport de diagnostic affiché.</summary>
/// <param name="Label">Intitulé.</param>
/// <param name="Value">Valeur réelle.</param>
public sealed record DiagnosticsRow(string Label, string Value);
