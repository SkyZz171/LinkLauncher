namespace LinkLauncher.App.Services;

using System.Windows;
using LinkLauncher.App.Views;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Models;

/// <summary>Présente les notifications dans une fenêtre discrète de LinkLauncher.</summary>
/// <remarks>
/// Aucune dépendance supplémentaire : la notification respecte le design system et ne s'appuie
/// pas sur une identité de package Windows. Elle est affichée sans voler le focus.
/// </remarks>
public sealed class WpfNotificationPresenter : INotificationPresenter
{
    /// <inheritdoc />
    public Task ShowAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(() => Show(notification)).Task;
    }

    private static void Show(AppNotification notification)
    {
        var window = new NotificationWindow(notification);

        // Plusieurs notifications peuvent se suivre : la plus récente reste au premier plan.
        window.Show();
        window.Activate();
    }
}
