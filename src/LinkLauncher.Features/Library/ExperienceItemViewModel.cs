namespace LinkLauncher.Features.Library;

using System.Globalization;
using LinkLauncher.Core.Models;

/// <summary>Expérience de la bibliothèque, mise en forme pour l'affichage.</summary>
public sealed class ExperienceItemViewModel
{
    /// <summary>Crée l'élément affiché à partir d'une entrée de la bibliothèque.</summary>
    /// <param name="experience">Entrée source.</param>
    public ExperienceItemViewModel(Experience experience)
    {
        ArgumentNullException.ThrowIfNull(experience);

        Id = experience.Id;
        Name = experience.Name;
        Url = experience.Url;
        Details = experience.PlaceId is { } placeId
            ? $"Lieu {placeId.ToString(CultureInfo.InvariantCulture)} · ajoutée le {experience.AddedAt:dd/MM/yyyy}"
            : $"Ajoutée le {experience.AddedAt:dd/MM/yyyy}";
        OpenedText = experience.OpenedAt is { } openedAt
            ? $"Dernière ouverture : {openedAt:dd/MM/yyyy à HH:mm}"
            : "Jamais ouverte depuis LinkLauncher";
    }

    /// <summary>Identifiant de l'entrée.</summary>
    public Guid Id { get; }

    /// <summary>Nom affiché.</summary>
    public string Name { get; }

    /// <summary>Page officielle conservée.</summary>
    public string Url { get; }

    /// <summary>Lieu et date d'ajout.</summary>
    public string Details { get; }

    /// <summary>Dernière ouverture de la page officielle.</summary>
    public string OpenedText { get; }
}
