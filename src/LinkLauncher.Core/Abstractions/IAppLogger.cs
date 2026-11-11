namespace LinkLauncher.Core.Abstractions;

/// <summary>Journalisation applicative de LinkLauncher.</summary>
public interface IAppLogger
{
    /// <summary>Enregistre une entrée de journal.</summary>
    /// <param name="level">Niveau de gravité.</param>
    /// <param name="category">Composant à l'origine du message.</param>
    /// <param name="message">Message lisible.</param>
    /// <param name="exception">Exception associée, le cas échéant.</param>
    void Log(AppLogLevel level, string category, string message, Exception? exception = null);
}
