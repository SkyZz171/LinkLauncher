namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Règles pures de la bibliothèque : validation, normalisation et modifications.</summary>
/// <remarks>
/// Aucune entrée n'est modifiée en place : chaque opération renvoie une nouvelle bibliothèque ou
/// un refus explicite, ce qui rend les règles testables et le stockage déterministe.
/// </remarks>
public static class LauncherLibraryRules
{
    /// <summary>Nombre maximal de profils conservés.</summary>
    public const int MaximumProfiles = 20;

    /// <summary>Nombre maximal d'expériences conservées.</summary>
    public const int MaximumExperiences = 500;

    /// <summary>Longueur maximale d'un nom.</summary>
    public const int MaximumNameLength = 60;

    /// <summary>Nom du profil créé automatiquement.</summary>
    public const string DefaultProfileName = "Par défaut";

    /// <summary>Crée une bibliothèque vierge dotée du profil par défaut.</summary>
    /// <param name="now">Horodatage de création.</param>
    public static LauncherLibrary CreateDefault(DateTimeOffset now)
    {
        var profile = new LaunchProfile
        {
            Id = Guid.NewGuid(),
            Name = DefaultProfileName,
            Locale = null,
            CreatedAt = now
        };

        return new LauncherLibrary
        {
            SchemaVersion = LauncherLibrary.CurrentSchemaVersion,
            Profiles = [profile],
            Experiences = [],
            ActiveProfileId = profile.Id
        };
    }

    /// <summary>Profil actif, ou premier profil disponible.</summary>
    /// <param name="library">Bibliothèque examinée.</param>
    public static LaunchProfile? ResolveActiveProfile(LauncherLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (library.Profiles.Count == 0)
        {
            return null;
        }

        return library.Profiles.FirstOrDefault(profile => profile.Id == library.ActiveProfileId)
            ?? library.Profiles[0];
    }

    /// <summary>
    /// Nettoie une bibliothèque lue sur le disque : entrées invalides écartées, doublons supprimés,
    /// noms et langues normalisés, profil actif cohérent.
    /// </summary>
    /// <param name="library">Bibliothèque lue.</param>
    /// <param name="now">Horodatage utilisé si un profil par défaut doit être recréé.</param>
    public static LauncherLibrary Normalize(LauncherLibrary library, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(library);

        var profiles = new List<LaunchProfile>();

        foreach (var profile in library.Profiles ?? [])
        {
            if (profile is null || profile.Id == Guid.Empty || profiles.Count >= MaximumProfiles)
            {
                continue;
            }

            if (profiles.Any(existing => existing.Id == profile.Id))
            {
                continue;
            }

            var name = CleanName(profile.Name);
            if (name is null)
            {
                continue;
            }

            var locale = RobloxLaunchUriBuilder.TryNormalizeLocale(profile.Locale, out var normalizedLocale)
                ? normalizedLocale
                : null;

            profiles.Add(profile with { Name = name, Locale = locale });
        }

        if (profiles.Count == 0)
        {
            return CreateDefault(now) with { Experiences = NormalizeExperiences(library.Experiences) };
        }

        var activeProfileId = profiles.Any(profile => profile.Id == library.ActiveProfileId)
            ? library.ActiveProfileId
            : profiles[0].Id;

        return new LauncherLibrary
        {
            SchemaVersion = LauncherLibrary.CurrentSchemaVersion,
            Profiles = profiles,
            Experiences = NormalizeExperiences(library.Experiences),
            ActiveProfileId = activeProfileId
        };
    }

    /// <summary>Ajoute un profil de lancement.</summary>
    public static LibraryMutation AddProfile(LauncherLibrary library, string name, string? locale, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(library);

        var cleanName = CleanName(name);
        if (cleanName is null)
        {
            return LibraryMutation.Failure(
                library,
                $"Le nom du profil est obligatoire ({MaximumNameLength} caractères maximum).");
        }

        if (library.Profiles.Any(profile => string.Equals(profile.Name, cleanName, StringComparison.CurrentCultureIgnoreCase)))
        {
            return LibraryMutation.Failure(library, "Un profil porte déjà ce nom.");
        }

        if (library.Profiles.Count >= MaximumProfiles)
        {
            return LibraryMutation.Failure(library, $"La limite de {MaximumProfiles} profils est atteinte.");
        }

        string? normalizedLocale = null;
        if (!string.IsNullOrWhiteSpace(locale) &&
            !RobloxLaunchUriBuilder.TryNormalizeLocale(locale, out normalizedLocale))
        {
            return LibraryMutation.Failure(library, "La langue doit ressembler à fr_FR ou en-US.");
        }

        var profile = new LaunchProfile
        {
            Id = Guid.NewGuid(),
            Name = cleanName,
            Locale = normalizedLocale,
            CreatedAt = now
        };

        return LibraryMutation.Success(library with { Profiles = [.. library.Profiles, profile] });
    }

    /// <summary>Supprime un profil ; le dernier profil ne peut pas être supprimé.</summary>
    public static LibraryMutation RemoveProfile(LauncherLibrary library, Guid profileId)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (library.Profiles.Count <= 1)
        {
            return LibraryMutation.Failure(library, "Le dernier profil ne peut pas être supprimé.");
        }

        if (!library.Profiles.Any(profile => profile.Id == profileId))
        {
            return LibraryMutation.Failure(library, "Ce profil n'existe plus.");
        }

        var remaining = library.Profiles.Where(profile => profile.Id != profileId).ToArray();
        var activeProfileId = library.ActiveProfileId == profileId
            ? remaining[0].Id
            : library.ActiveProfileId;

        return LibraryMutation.Success(library with { Profiles = remaining, ActiveProfileId = activeProfileId });
    }

    /// <summary>Active un profil existant.</summary>
    public static LibraryMutation SetActiveProfile(LauncherLibrary library, Guid profileId)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (!library.Profiles.Any(profile => profile.Id == profileId))
        {
            return LibraryMutation.Failure(library, "Ce profil n'existe plus.");
        }

        return LibraryMutation.Success(library with { ActiveProfileId = profileId });
    }

    /// <summary>Ajoute une expérience à partir d'un lien officiel ou d'un identifiant de lieu.</summary>
    public static LibraryMutation AddExperience(LauncherLibrary library, string name, string link, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(library);

        var cleanName = CleanName(name);
        if (cleanName is null)
        {
            return LibraryMutation.Failure(
                library,
                $"Le nom de l'expérience est obligatoire ({MaximumNameLength} caractères maximum).");
        }

        if (!RobloxExperienceLink.TryCreate(link, out var reference) || reference is null)
        {
            return LibraryMutation.Failure(library, RobloxExperienceLink.InvalidMessage);
        }

        if (library.Experiences.Count >= MaximumExperiences)
        {
            return LibraryMutation.Failure(library, $"La limite de {MaximumExperiences} expériences est atteinte.");
        }

        var url = reference.Page.AbsoluteUri;
        if (library.Experiences.Any(experience => string.Equals(experience.Url, url, StringComparison.OrdinalIgnoreCase)))
        {
            return LibraryMutation.Failure(library, "Cette expérience est déjà dans la bibliothèque.");
        }

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            Name = cleanName,
            Url = url,
            PlaceId = reference.PlaceId,
            AddedAt = now
        };

        return LibraryMutation.Success(library with { Experiences = [.. library.Experiences, experience] });
    }

    /// <summary>Retire une expérience de la bibliothèque.</summary>
    public static LibraryMutation RemoveExperience(LauncherLibrary library, Guid experienceId)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (!library.Experiences.Any(experience => experience.Id == experienceId))
        {
            return LibraryMutation.Failure(library, "Cette expérience n'existe plus.");
        }

        return LibraryMutation.Success(library with
        {
            Experiences = library.Experiences.Where(experience => experience.Id != experienceId).ToArray()
        });
    }

    /// <summary>Note l'ouverture de la page officielle d'une expérience.</summary>
    public static LibraryMutation MarkExperienceOpened(LauncherLibrary library, Guid experienceId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(library);

        var experience = library.Experiences.FirstOrDefault(item => item.Id == experienceId);
        if (experience is null)
        {
            return LibraryMutation.Failure(library, "Cette expérience n'existe plus.");
        }

        return LibraryMutation.Success(library with
        {
            Experiences = library.Experiences
                .Select(item => item.Id == experienceId ? item with { OpenedAt = now } : item)
                .ToArray()
        });
    }

    private static IReadOnlyList<Experience> NormalizeExperiences(IReadOnlyList<Experience>? experiences)
    {
        var normalized = new List<Experience>();

        foreach (var experience in experiences ?? [])
        {
            if (experience is null || experience.Id == Guid.Empty || normalized.Count >= MaximumExperiences)
            {
                continue;
            }

            if (normalized.Any(existing => existing.Id == experience.Id))
            {
                continue;
            }

            var name = CleanName(experience.Name);
            if (name is null || !RobloxExperienceLink.TryCreate(experience.Url, out var reference) || reference is null)
            {
                continue;
            }

            if (normalized.Any(existing => string.Equals(existing.Url, reference.Page.AbsoluteUri, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            normalized.Add(experience with
            {
                Name = name,
                Url = reference.Page.AbsoluteUri,
                PlaceId = reference.PlaceId
            });
        }

        return normalized;
    }

    private static string? CleanName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        return trimmed.Length > MaximumNameLength ? trimmed[..MaximumNameLength].Trim() : trimmed;
    }
}
