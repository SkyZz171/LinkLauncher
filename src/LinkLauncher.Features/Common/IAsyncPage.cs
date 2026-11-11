namespace LinkLauncher.Features.Common;

/// <summary>Page dont l'affichage nécessite un chargement initial.</summary>
public interface IAsyncPage
{
    /// <summary>Charge les données nécessaires à la page. Peut être appelée plusieurs fois sans effet de bord.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
