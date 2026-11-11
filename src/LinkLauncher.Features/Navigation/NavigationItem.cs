namespace LinkLauncher.Features.Navigation;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>Entrée de la barre latérale de navigation.</summary>
public sealed partial class NavigationItem : ObservableObject
{
    /// <summary>Crée un élément de navigation.</summary>
    /// <param name="route">Route associée.</param>
    /// <param name="label">Libellé affiché.</param>
    /// <param name="glyph">Glyphe de la police d'icônes Segoe.</param>
    public NavigationItem(string route, string label, string glyph)
    {
        Route = route;
        Label = label;
        Glyph = glyph;
    }

    /// <summary>Route associée.</summary>
    public string Route { get; }

    /// <summary>Libellé affiché.</summary>
    public string Label { get; }

    /// <summary>Glyphe de la police d'icônes.</summary>
    public string Glyph { get; }

    /// <summary>Indique si l'élément correspond à la page affichée.</summary>
    [ObservableProperty]
    private bool _isSelected;
}
