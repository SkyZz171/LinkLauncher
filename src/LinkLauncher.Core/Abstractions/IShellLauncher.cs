namespace LinkLauncher.Core.Abstractions;

/// <summary>
/// Demande à Windows d'ouvrir une cible via son association (protocole, URL, exécutable).
/// </summary>
/// <remarks>
/// Aucun processus n'est surveillé ni manipulé par ce service : il transmet la demande au shell.
/// </remarks>
public interface IShellLauncher
{
    /// <summary>Ouvre une cible associée, par exemple un protocole enregistré ou une URL.</summary>
    /// <param name="target">Cible à ouvrir.</param>
    ShellLaunchResult Launch(string target);
}
