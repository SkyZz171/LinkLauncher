namespace LinkLauncher.Features.Library;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using LinkLauncher.Features.Common;

/// <summary>
/// Présente la bibliothèque d'expériences et les profils de lancement, et n'ouvre que des pages
/// officielles roblox.com.
/// </summary>
public sealed partial class LibraryViewModel : ObservableObject, IAsyncPage
{
    private const string Category = "Bibliothèque";

    private readonly ILauncherLibrary _library;
    private readonly IShellLauncher _shellLauncher;
    private readonly IAppLogger _logger;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    private bool _isSyncingSelection;

    /// <summary>Crée le ViewModel de la bibliothèque.</summary>
    public LibraryViewModel(
        ILauncherLibrary library,
        IShellLauncher shellLauncher,
        IAppPaths paths,
        IAppLogger logger)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _shellLauncher = shellLauncher ?? throw new ArgumentNullException(nameof(shellLauncher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(paths);

        LibraryFilePath = paths.LibraryFilePath;

        _library.Changed += OnLibraryChanged;
        ApplyLibrary(_library.Current);
    }

    /// <summary>Expériences enregistrées.</summary>
    public ObservableCollection<ExperienceItemViewModel> Experiences { get; } = [];

    /// <summary>Profils de lancement disponibles.</summary>
    public ObservableCollection<ProfileOption> Profiles { get; } = [];

    /// <summary>Emplacement du fichier de bibliothèque.</summary>
    public string LibraryFilePath { get; }

    /// <summary>Nom saisi pour une nouvelle expérience.</summary>
    [ObservableProperty]
    private string _newExperienceName = string.Empty;

    /// <summary>Lien ou identifiant de lieu saisi.</summary>
    [ObservableProperty]
    private string _newExperienceLink = string.Empty;

    /// <summary>Nom saisi pour un nouveau profil.</summary>
    [ObservableProperty]
    private string _newProfileName = string.Empty;

    /// <summary>Langue saisie pour un nouveau profil.</summary>
    [ObservableProperty]
    private string _newProfileLocale = string.Empty;

    /// <summary>Profil sélectionné dans la liste.</summary>
    [ObservableProperty]
    private ProfileOption? _selectedProfile;

    /// <summary>Résumé du profil réellement appliqué au lancement.</summary>
    [ObservableProperty]
    private string _activeProfileSummary = "Aucun profil de lancement.";

    /// <summary>Message d'erreur présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Message de confirmation présenté à l'utilisateur.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Indique que la bibliothèque contient au moins une expérience.</summary>
    [ObservableProperty]
    private bool _hasExperiences;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_library.IsLoaded)
        {
            try
            {
                await _library.InitializeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(Category, "Chargement de la bibliothèque impossible.", ex);
                ErrorMessage = "La bibliothèque n'a pas pu être chargée. Le détail technique figure dans le journal d'activité.";
            }
        }

        ApplyLibrary(_library.Current);
    }

    [RelayCommand]
    private async Task AddExperienceAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            () => _library.AddExperienceAsync(NewExperienceName, NewExperienceLink, cancellationToken),
            "Expérience ajoutée à la bibliothèque.",
            () =>
            {
                NewExperienceName = string.Empty;
                NewExperienceLink = string.Empty;
            });
    }

    [RelayCommand]
    private async Task AddProfileAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            () => _library.AddProfileAsync(NewProfileName, NewProfileLocale, cancellationToken),
            "Profil de lancement ajouté.",
            () =>
            {
                NewProfileName = string.Empty;
                NewProfileLocale = string.Empty;
            });
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSelectedProfile))]
    private async Task RemoveProfileAsync(CancellationToken cancellationToken)
    {
        if (SelectedProfile is not { } profile)
        {
            return;
        }

        await ExecuteAsync(
            () => _library.RemoveProfileAsync(profile.Id, cancellationToken),
            "Profil de lancement supprimé.");
    }

    [RelayCommand]
    private async Task OpenExperienceAsync(ExperienceItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        ErrorMessage = null;
        StatusMessage = null;

        var result = _shellLauncher.Launch(item.Url);
        if (!result.Succeeded)
        {
            _logger.Error(
                Category,
                "Ouverture de la page officielle impossible.",
                new InvalidOperationException(result.ErrorMessage ?? "Erreur inconnue"));
            ErrorMessage = "La page officielle n'a pas pu être ouverte. Vérifiez votre navigateur par défaut.";
            return;
        }

        try
        {
            var mutation = await _library.MarkExperienceOpenedAsync(item.Id);
            if (!mutation.Succeeded)
            {
                _logger.Warn(Category, mutation.Error ?? "Ouverture non enregistrée.");
            }
        }
        catch (Exception ex)
        {
            // L'ouverture a bien eu lieu : seul son horodatage n'a pas pu être enregistré.
            _logger.Error(Category, "Enregistrement de l'ouverture impossible.", ex);
        }

        StatusMessage = $"Page officielle de « {item.Name} » ouverte dans le navigateur.";
        _logger.Info(Category, $"Page officielle ouverte : {item.Url}");
    }

    [RelayCommand]
    private async Task RemoveExperienceAsync(ExperienceItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await ExecuteAsync(
            () => _library.RemoveExperienceAsync(item.Id),
            $"« {item.Name} » a été retirée de la bibliothèque.");
    }

    partial void OnSelectedProfileChanged(ProfileOption? value)
    {
        if (_isSyncingSelection || value is null)
        {
            return;
        }

        ActivateProfile(value);
    }

    private bool CanRemoveSelectedProfile() => SelectedProfile is not null && Profiles.Count > 1;

    private async Task ExecuteAsync(
        Func<Task<LibraryMutation>> operation,
        string successMessage,
        Action? onSuccess = null)
    {
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            var mutation = await operation();

            if (!mutation.Succeeded)
            {
                ErrorMessage = mutation.Error ?? "La modification a été refusée.";
                return;
            }

            onSuccess?.Invoke();
            StatusMessage = successMessage;
            _logger.Info(Category, successMessage);
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Enregistrement de la bibliothèque impossible.", ex);
            ErrorMessage = "La modification n'a pas pu être enregistrée sur le disque. Le détail technique figure dans le journal d'activité.";
        }
    }

    private async void ActivateProfile(ProfileOption profile)
    {
        ErrorMessage = null;

        try
        {
            var mutation = await _library.SetActiveProfileAsync(profile.Id);
            if (!mutation.Succeeded)
            {
                ErrorMessage = mutation.Error;
                ApplyLibrary(_library.Current);
                return;
            }

            StatusMessage = $"Profil de lancement actif : {profile.Label}.";
        }
        catch (Exception ex)
        {
            _logger.Error(Category, "Activation du profil impossible.", ex);
            ErrorMessage = "Le profil n'a pas pu être activé. Le détail technique figure dans le journal d'activité.";
            ApplyLibrary(_library.Current);
        }
    }

    private void OnLibraryChanged(object? sender, EventArgs e) =>
        PostToUi(() => ApplyLibrary(_library.Current));

    private void PostToUi(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }

    private void ApplyLibrary(LauncherLibrary library)
    {
        var activeProfile = LauncherLibraryRules.ResolveActiveProfile(library);
        var activeProfileId = activeProfile?.Id;

        Experiences.Clear();
        foreach (var experience in library.Experiences)
        {
            Experiences.Add(new ExperienceItemViewModel(experience));
        }

        _isSyncingSelection = true;
        try
        {
            Profiles.Clear();
            foreach (var profile in library.Profiles)
            {
                Profiles.Add(new ProfileOption(
                    profile.Id,
                    profile.Name,
                    profile.Locale is null ? "Langue du système Windows" : $"Langue transmise : {profile.Locale}"));
            }

            SelectedProfile = Profiles.FirstOrDefault(option => option.Id == activeProfileId)
                ?? Profiles.FirstOrDefault();
        }
        finally
        {
            _isSyncingSelection = false;
        }

        HasExperiences = Experiences.Count > 0;
        ActiveProfileSummary = activeProfile switch
        {
            null => "Aucun profil de lancement.",
            { Locale: null } => $"Profil actif : {activeProfile.Name} · langue du système",
            _ => $"Profil actif : {activeProfile.Name} · langue {activeProfile.Locale}"
        };

        RemoveProfileCommand.NotifyCanExecuteChanged();
    }
}
