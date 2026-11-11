namespace LinkLauncher.Core.Abstractions;

using LinkLauncher.Core.Models;

/// <summary>Présente une notification locale à l'utilisateur.</summary>
public interface INotificationPresenter
{
    /// <summary>Affiche une notification ; un échec ne doit pas interrompre l'application.</summary>
    /// <param name="notification">Notification à présenter.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task ShowAsync(AppNotification notification, CancellationToken cancellationToken = default);
}
