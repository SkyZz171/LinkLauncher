namespace LinkLauncher.Features.Activity;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Services;
using LinkLauncher.Features.Common;

/// <summary>Présente le temps d'utilisation réellement observé du client Roblox.</summary>
public sealed partial class ActivityViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Activité";

    private readonly IActivityHistory _history;
    private readonly IAppLogger _logger;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    private DateTimeOffset? _clearRequestedAt;

    /// <summary>Crée le ViewModel de la page Activité.</summary>
    public ActivityViewModel(IActivityHistory history, IAppLogger logger)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _history.Changed += OnHistoryChanged;
    }

    /// <summary>Sessions observées, de la plus récente à la plus ancienne.</summary>
    public ObservableCollection<SessionRow> Sessions { get; } = [];

    /// <summary>Temps observé aujourd'hui.</summary>
    [ObservableProperty]
    private string _todayText = "0 min";

    /// <summary>Temps observé sur les sept derniers jours.</summary>
    [ObservableProperty]
    private string _weekText = "0 min";

    /// <summary>Temps observé depuis le début de l'historique.</summary>
    [ObservableProperty]
    private string _totalText = "0 min";

    /// <summary>Nombre de sessions enregistrées.</summary>
    [ObservableProperty]
    private string _sessionCountText = "Aucune session enregistrée";

    /// <summary>Dernière session observée.</summary>
    [ObservableProperty]
    private string _lastSessionText = "Aucune session enregistrée";

    /// <summary>Indique que l'historique est vide.</summary>
    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>Message de confirmation ou d'information.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        Refresh();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Refresh()
    {
        var now = DateTimeOffset.Now;
        var summary = ActivityHistoryRules.Summarize(_history.Current, now);

        TodayText = ActivityHistoryRules.FormatDuration(summary.Today);
        WeekText = ActivityHistoryRules.FormatDuration(summary.LastSevenDays);
        TotalText = ActivityHistoryRules.FormatDuration(summary.Total);

        SessionCountText = summary.SessionCount switch
        {
            0 => "Aucune session enregistrée",
            1 => "1 session enregistrée",
            _ => $"{summary.SessionCount} sessions enregistrées"
        };

        LastSessionText = summary.LastSessionAt is { } last
            ? $"Dernière session terminée à {last.ToLocalTime():HH:mm}"
            : "Aucune session terminée pour l'instant";

        Sessions.Clear();

        foreach (var session in _history.Current.Sessions
                     .OrderByDescending(item => item.StartedAt)
                     .Take(30))
        {
            Sessions.Add(SessionRow.From(session, now));
        }

        IsEmpty = Sessions.Count == 0;
    }

    [RelayCommand]
    private async Task ClearHistoryAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (_clearRequestedAt is null || DateTimeOffset.Now - _clearRequestedAt > TimeSpan.FromSeconds(8))
        {
            _clearRequestedAt = DateTimeOffset.Now;
            StatusMessage = "Cliquez à nouveau sur « Effacer » pour supprimer définitivement cet historique local.";
            return;
        }

        _clearRequestedAt = null;

        try
        {
            await _history.ClearAsync(cancellationToken);
            StatusMessage = "Historique effacé.";
            _logger.Info(Category, "Historique d'activité effacé par l'utilisateur.");
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Effacement de l'historique impossible.", ex);
            ErrorMessage = "L'historique n'a pas pu être effacé. Le détail technique figure dans le journal d'activité.";
        }
    }

    private void OnHistoryChanged(object? sender, EventArgs e) => PostToUi(Refresh);

    private void PostToUi(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }
}
