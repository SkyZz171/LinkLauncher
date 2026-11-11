namespace LinkLauncher.Features.Activity;

using System.Globalization;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>Ligne affichée pour une session observée.</summary>
/// <param name="StartedAt">Début de la session.</param>
/// <param name="When">Date et heure lisibles.</param>
/// <param name="Duration">Durée observée.</param>
/// <param name="Detail">Profil et code de sortie, ou état de la session.</param>
/// <param name="IsOpen">Indique que la session est en cours.</param>
public sealed record SessionRow(
    DateTimeOffset StartedAt,
    string When,
    string Duration,
    string Detail,
    bool IsOpen)
{
    /// <summary>Construit la ligne affichée à partir d'une session enregistrée.</summary>
    /// <param name="session">Session enregistrée.</param>
    /// <param name="now">Moment de référence.</param>
    public static SessionRow From(PlaySession session, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        var when = session.StartedAt.ToLocalTime().ToString("ddd d MMM · HH:mm", CultureInfo.CurrentCulture);

        var duration = session.ObservedDuration is { } observed
            ? ActivityHistoryRules.FormatDuration(observed)
            : "durée inconnue";

        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(session.ProfileName))
        {
            details.Add($"profil {session.ProfileName}");
        }

        if (session.IsOpen)
        {
            details.Add("en cours");
        }
        else if (session.Interrupted)
        {
            details.Add("session interrompue, durée non comptée");
        }
        else if (session.ExitCode is { } exitCode)
        {
            details.Add($"code de sortie {exitCode.ToString(CultureInfo.InvariantCulture)}");
        }

        _ = now;

        return new SessionRow(
            session.StartedAt,
            when,
            duration,
            details.Count == 0 ? "—" : string.Join(" · ", details),
            session.IsOpen);
    }
}
