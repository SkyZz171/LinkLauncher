namespace LinkLauncher.Core.Services;

using LinkLauncher.Core.Models;

/// <summary>Règles de constitution et d'agrégation de l'historique des sessions.</summary>
public static class ActivityHistoryRules
{
    /// <summary>Nombre maximal de sessions conservées.</summary>
    public const int MaximumSessions = 200;

    /// <summary>Crée un historique vide.</summary>
    public static ActivityHistory CreateDefault() => new();

    /// <summary>Ramène un historique lu sur disque à un état exploitable.</summary>
    /// <param name="history">Historique lu, éventuellement incomplet.</param>
    /// <param name="now">Moment de référence.</param>
    /// <remarks>
    /// Une session encore ouverte provient d'une exécution précédente interrompue : elle est
    /// marquée comme telle et sa durée n'est jamais déduite d'une heure de fin inventée.
    /// </remarks>
    public static ActivityHistory Normalize(ActivityHistory? history, DateTimeOffset now)
    {
        if (history is null)
        {
            return CreateDefault();
        }

        var sessions = history.Sessions
            .Where(session => session is not null)
            .Select(session => session.EndedAt is null && !session.Interrupted
                ? session with { Interrupted = true }
                : session)
            .Where(session => session.StartedAt <= now.AddDays(1))
            .OrderBy(session => session.StartedAt)
            .ToList();

        if (sessions.Count > MaximumSessions)
        {
            sessions = sessions.Skip(sessions.Count - MaximumSessions).ToList();
        }

        return new ActivityHistory
        {
            SchemaVersion = ActivityHistory.CurrentSchemaVersion,
            Sessions = sessions
        };
    }

    /// <summary>Ouvre une session pour le processus observé.</summary>
    /// <param name="history">Historique courant.</param>
    /// <param name="process">Processus Roblox observé.</param>
    /// <param name="experienceName">Expérience ouverte depuis LinkLauncher, si connue.</param>
    /// <param name="profileName">Profil de lancement actif, si connu.</param>
    /// <param name="now">Moment de référence.</param>
    /// <remarks>Un même processus n'ouvre jamais deux sessions : l'appel est sans effet sinon.</remarks>
    public static ActivityHistory StartSession(
        ActivityHistory history,
        RobloxProcessInfo process,
        string? experienceName,
        string? profileName,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(process);

        if (history.Sessions.Any(session => session.IsOpen && session.ProcessId == process.ProcessId))
        {
            return history;
        }

        var session = new PlaySession
        {
            StartedAt = process.StartedAt ?? now,
            ProcessId = process.ProcessId,
            ExperienceName = string.IsNullOrWhiteSpace(experienceName) ? null : experienceName.Trim(),
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? null : profileName.Trim()
        };

        var sessions = history.Sessions.Append(session).OrderBy(item => item.StartedAt).ToList();

        if (sessions.Count > MaximumSessions)
        {
            sessions = sessions.Skip(sessions.Count - MaximumSessions).ToList();
        }

        return history with { Sessions = sessions };
    }

    /// <summary>Clôt la session ouverte correspondant au processus observé.</summary>
    /// <param name="history">Historique courant.</param>
    /// <param name="process">Processus Roblox fermé.</param>
    /// <param name="now">Moment de référence.</param>
    public static ActivityHistory EndSession(ActivityHistory history, RobloxProcessInfo process, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(process);

        var index = history.Sessions.FindLastIndex(
            session => session.IsOpen && session.ProcessId == process.ProcessId);

        if (index < 0)
        {
            return history;
        }

        var closed = history.Sessions[index] with
        {
            EndedAt = now < history.Sessions[index].StartedAt ? history.Sessions[index].StartedAt : now,
            ExitCode = process.ExitCode
        };

        var sessions = new List<PlaySession>(history.Sessions);
        sessions[index] = closed;

        return history with { Sessions = sessions };
    }

    /// <summary>Calcule les totaux affichés à partir des sessions observées.</summary>
    /// <param name="history">Historique courant.</param>
    /// <param name="now">Moment de référence.</param>
    public static ActivitySummary Summarize(ActivityHistory history, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(history);

        var dayStart = new DateTimeOffset(now.Date, now.Offset);
        var todayStart = dayStart;
        var weekStart = now.AddDays(-7);
        var closed = history.Sessions.Where(session => !session.Interrupted && session.ObservedDuration is not null);

        var total = TimeSpan.Zero;
        var today = TimeSpan.Zero;
        var week = TimeSpan.Zero;
        DateTimeOffset? lastSessionAt = null;

        foreach (var session in closed)
        {
            var ended = session.EndedAt!.Value;
            var duration = session.ObservedDuration!.Value;
            var clampedEnd = ended > now ? now : ended;

            total += duration;

            if (clampedEnd > todayStart)
            {
                today += ClampSpan(session.StartedAt, clampedEnd, todayStart, now);
            }

            if (clampedEnd > weekStart)
            {
                week += ClampSpan(session.StartedAt, clampedEnd, weekStart, now);
            }

            if (lastSessionAt is null || ended > lastSessionAt)
            {
                lastSessionAt = ended;
            }
        }

        return new ActivitySummary(today, week, total, history.Sessions.Count, lastSessionAt);
    }

    /// <summary>Formate une durée en heures et minutes pour l'affichage.</summary>
    /// <param name="duration">Durée à formater.</param>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "0 min";
        }

        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        if (hours == 0)
        {
            return minutes <= 0 ? "moins d'une minute" : $"{minutes} min";
        }

        return minutes == 0 ? $"{hours} h" : $"{hours} h {minutes:00}";
    }

    private static TimeSpan ClampSpan(DateTimeOffset started, DateTimeOffset ended, DateTimeOffset lowerBound, DateTimeOffset upperBound)
    {
        var from = started < lowerBound ? lowerBound : started;
        var to = ended > upperBound ? upperBound : ended;

        return to > from ? to - from : TimeSpan.Zero;
    }
}
