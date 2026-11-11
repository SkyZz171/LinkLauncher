namespace LinkLauncher.App.Views;

using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LinkLauncher.Core.Models;

/// <summary>Notification discrète affichée en bas à droite de la zone de travail.</summary>
public partial class NotificationWindow : Window
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(6);

    private readonly DispatcherTimer _timer;

    /// <summary>Crée la fenêtre pour une notification donnée.</summary>
    /// <param name="notification">Notification à présenter.</param>
    public NotificationWindow(AppNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        InitializeComponent();
        DataContext = notification;

        _timer = new DispatcherTimer { Interval = DisplayDuration };
        _timer.Tick += (_, _) => Dismiss();

        Loaded += OnLoaded;
        Closed += (_, _) => _timer.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnWorkArea();

        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        _timer.Start();
    }

    private void PositionOnWorkArea()
    {
        var workArea = SystemParameters.WorkArea;

        Left = workArea.Right - ActualWidth - 8;
        Top = workArea.Bottom - ActualHeight - 8;
    }

    private void OnDismiss(object sender, System.Windows.Input.MouseButtonEventArgs e) => Dismiss();

    private void Dismiss()
    {
        _timer.Stop();

        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140));
        animation.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, animation);
    }
}
