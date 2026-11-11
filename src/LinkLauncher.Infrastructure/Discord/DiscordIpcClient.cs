namespace LinkLauncher.Infrastructure.Discord;

using System.IO.Pipes;
using LinkLauncher.Core.Abstractions;
using LinkLauncher.Core.Services;

/// <summary>Transport IPC vers le client Discord local, par canal nommé.</summary>
/// <remarks>
/// Aucune connexion réseau : le protocole passe uniquement par les canaux nommés
/// <c>discord-ipc-0</c> à <c>discord-ipc-9</c> ouverts par le client Discord installé.
/// </remarks>
public sealed class DiscordIpcClient : IDiscordIpcTransport
{
    private const string Category = "Discord";
    private const int MaximumPipeIndex = 9;
    private const int ConnectTimeoutMilliseconds = 250;

    private readonly IAppLogger _logger;
    private NamedPipeClientStream? _pipe;

    /// <summary>Crée le transport IPC.</summary>
    public DiscordIpcClient(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _pipe?.IsConnected == true;

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);

        for (var index = 0; index <= MaximumPipeIndex; index++)
        {
            var pipe = new NamedPipeClientStream(
                ".",
                $"discord-ipc-{index}",
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.ConnectAsync(ConnectTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
                _pipe = pipe;

                if (!await TrySendAsync(DiscordIpcMessages.EncodeHandshake(applicationId), cancellationToken)
                        .ConfigureAwait(false))
                {
                    await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }

                var response = await ReadFrameAsync(cancellationToken).ConfigureAwait(false);
                if (response is null)
                {
                    await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }

                if (response.Opcode == DiscordIpcMessages.CloseOpcode)
                {
                    _logger.Warn(
                        Category,
                        $"Discord a refusé la connexion : {DiscordIpcMessages.DescribeError(response.Json)}");
                    await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }

                _logger.Info(Category, "Canal IPC Discord ouvert.");
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                pipe.Dispose();
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> TrySendAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_pipe is null || !_pipe.IsConnected)
        {
            return false;
        }

        try
        {
            await _pipe.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            await _pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            _logger.Warn(Category, $"Envoi d'une trame Discord impossible : {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisposePipe();
        return Task.CompletedTask;
    }

    /// <summary>Ferme le canal.</summary>
    public void Dispose() => DisposePipe();

    private async Task<DiscordIpcFrame?> ReadFrameAsync(CancellationToken cancellationToken)
    {
        if (_pipe is null)
        {
            return null;
        }

        var header = new byte[8];
        await _pipe.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);

        var payloadLength = BitConverter.ToInt32(header, 4);
        if (payloadLength is < 0 or > 65_536)
        {
            return null;
        }

        var payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            await _pipe.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        }

        var frame = new byte[8 + payloadLength];
        header.CopyTo(frame, 0);
        payload.CopyTo(frame, 8);

        return DiscordIpcMessages.TryReadFrame(frame, frame.Length, out var decoded, out _) ? decoded : null;
    }

    private void DisposePipe()
    {
        var pipe = _pipe;
        _pipe = null;

        if (pipe is null)
        {
            return;
        }

        try
        {
            pipe.Dispose();
        }
        catch (IOException ex)
        {
            _logger.Warn(Category, $"Fermeture du canal IPC Discord incomplète : {ex.Message}");
        }
    }
}
