namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Sélectionne le dossier de version Roblox le plus récent.</summary>
public static class RobloxBuildSelector
{
    /// <summary>
    /// Choisit le candidat dont la version est la plus élevée. Les versions illisibles sont
    /// classées après les versions exploitables, puis départagées par date d'écriture.
    /// </summary>
    /// <param name="candidates">Candidats détectés sur la machine.</param>
    /// <returns>Le candidat retenu, ou <see langword="null"/> si la liste est vide.</returns>
    public static RobloxBuildCandidate? SelectPreferred(IEnumerable<RobloxBuildCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .OrderByDescending(candidate => RobloxVersionParser.TryParse(candidate.FileVersion))
            .ThenByDescending(candidate => candidate.LastWriteTimeUtc)
            .ThenBy(candidate => candidate.FolderName, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
