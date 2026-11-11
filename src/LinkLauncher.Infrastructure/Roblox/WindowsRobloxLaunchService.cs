namespace LinkLauncher.Infrastructure.Roblox;

using System.Globalization;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;

/// <summary>
/// Lance le client Roblox par le protocole officiel enregistré par Roblox.
/// </summary>
/// <remarks>
/// Aucun identifiant n'est utilisé : l'URI <c>roblox-player:1+launchmode:app</c> démarre le client
/// sans session, exactement comme le raccourci officiel « Roblox Player ». Rejoindre une expérience
/// suppose un ticket d'authentification produit par le site Roblox dans la session du navigateur,
/// ce que LinkLauncher ne fait jamais à la place de l'utilisateur.
/// </remarks>
public sealed class WindowsRobloxLaunchService : IRobloxLaunchService
{
    private const string Category = "Lancement";
    private const string PlayerProtocolScheme = "roblox-player";

    private readonly IRobloxProbe _probe;
    private readonly IRobloxProcessSource _processSource;
    private readonly IProtocolRegistrationReader _protocolReader;
    private readonly IShellLauncher _shellLauncher;
    private readonly IActiveProfileProvider _activeProfileProvider;
    private readonly IAppLogger _logger;

    /// <summary>Crée le service de lancement.</summary>
    public WindowsRobloxLaunchService(
        IRobloxProbe probe,
        IRobloxProcessSource processSource,
        IProtocolRegistrationReader protocolReader,
        IShellLauncher shellLauncher,
        IActiveProfileProvider activeProfileProvider,
        IAppLogger logger)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _processSource = processSource ?? throw new ArgumentNullException(nameof(processSource));
        _protocolReader = protocolReader ?? throw new ArgumentNullException(nameof(protocolReader));
        _shellLauncher = shellLauncher ?? throw new ArgumentNullException(nameof(shellLauncher));
        _activeProfileProvider = activeProfileProvider ?? throw new ArgumentNullException(nameof(activeProfileProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RobloxLaunchResult> LaunchClientAsync(CancellationToken cancellationToken = default)
    {
        var running = _processSource.GetRunningProcesses();
        if (running.Count > 0)
        {
            _logger.Info(Category, $"Le client Roblox est déjà lancé (PID {running[0].ProcessId}).");
            return RobloxLaunchResult.Success(RobloxLaunchOutcome.AlreadyRunning);
        }

        var installation = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
        if (!installation.IsUsable || installation.PlayerExecutablePath is null)
        {
            _logger.Warn(Category, $"Lancement refusé : installation inutilisable ({installation.State}).");
            return RobloxLaunchResult.Failure(RobloxLaunchOutcome.InstallationNotUsable, installation.Issue);
        }

        if (string.IsNullOrWhiteSpace(_protocolReader.TryGetCommand(PlayerProtocolScheme)))
        {
            _logger.Warn(Category, "Lancement refusé : protocole roblox-player non enregistré.");
            return RobloxLaunchResult.Failure(
                RobloxLaunchOutcome.ProtocolNotRegistered,
                @"Aucune association pour roblox-player dans HKCU\Software\Classes.");
        }

        var launchUri = RobloxLaunchUriBuilder.BuildClientLaunchUri(ResolveLaunchLocale());
        var shellResult = _shellLauncher.Launch(launchUri);

        if (!shellResult.Succeeded)
        {
            return RobloxLaunchResult.Failure(RobloxLaunchOutcome.ShellFailure, shellResult.ErrorMessage);
        }

        _logger.Info(Category, $"Demande de lancement transmise au client officiel ({launchUri}).");
        return RobloxLaunchResult.Success(RobloxLaunchOutcome.Launched);
    }

    /// <inheritdoc />
    public RobloxLaunchResult OpenOfficialDownloadPage()
    {
        var shellResult = _shellLauncher.Launch(RobloxWebUrls.OfficialDownloadPage.AbsoluteUri);

        if (!shellResult.Succeeded)
        {
            return RobloxLaunchResult.Failure(RobloxLaunchOutcome.ShellFailure, shellResult.ErrorMessage);
        }

        _logger.Info(Category, "Page de téléchargement officielle ouverte dans le navigateur par défaut.");
        return RobloxLaunchResult.Success(RobloxLaunchOutcome.Launched);
    }

    private string? ResolveLaunchLocale()
    {
        var profile = _activeProfileProvider.GetActiveProfile();

        return string.IsNullOrWhiteSpace(profile?.Locale)
            ? CultureInfo.CurrentUICulture.Name
            : profile.Locale;
    }
}
