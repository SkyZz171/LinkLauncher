namespace LinkLauncher.Infrastructure.FileSystem;

using LinkLauncher.Core.Abstractions;

/// <summary>Implémentation de <see cref="IFileSystem"/> reposant sur <see cref="System.IO"/>.</summary>
public sealed class SystemFileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);

    /// <inheritdoc />
    public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Move(sourcePath, destinationPath, overwrite);

    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Copy(sourcePath, destinationPath, overwrite);

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <inheritdoc />
    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
}
