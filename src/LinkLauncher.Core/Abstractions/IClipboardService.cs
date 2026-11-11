namespace LinkLauncher.Core.Abstractions;

/// <summary>Accès au presse-papiers, isolé pour rester testable.</summary>
public interface IClipboardService
{
    /// <summary>Copie un texte dans le presse-papiers.</summary>
    /// <param name="text">Texte à copier.</param>
    /// <returns><see langword="true"/> si la copie a réussi.</returns>
    bool TrySetText(string text);
}
