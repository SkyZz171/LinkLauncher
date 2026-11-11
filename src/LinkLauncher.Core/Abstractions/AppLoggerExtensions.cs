namespace LinkLauncher.Core.Abstractions;

/// <summary>Raccourcis de journalisation par niveau.</summary>
public static class AppLoggerExtensions
{
    /// <summary>Enregistre une information.</summary>
    public static void Info(this IAppLogger logger, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Log(AppLogLevel.Information, category, message);
    }

    /// <summary>Enregistre un avertissement.</summary>
    public static void Warn(this IAppLogger logger, string category, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Log(AppLogLevel.Warning, category, message);
    }

    /// <summary>Enregistre une erreur.</summary>
    public static void Error(this IAppLogger logger, string category, string message, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Log(AppLogLevel.Error, category, message, exception);
    }
}
