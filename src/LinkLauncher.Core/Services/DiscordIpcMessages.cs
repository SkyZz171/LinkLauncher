namespace LinkLauncher.Core.Services;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LinkLauncher.Core.Models;

/// <summary>Trame IPC Discord décodée.</summary>
/// <param name="Opcode">Code d'opération du protocole Discord.</param>
/// <param name="Json">Charge utile JSON.</param>
public sealed record DiscordIpcFrame(int Opcode, string Json);

/// <summary>Encode et décode les trames du protocole IPC local de Discord.</summary>
/// <remarks>
/// Le protocole est minimal : en-tête de deux entiers 32 bits little-endian (opération et taille),
/// suivi de la charge utile JSON encodée en UTF-8. Aucun accès réseau n'est utilisé.
/// </remarks>
public static class DiscordIpcMessages
{
    /// <summary>Opération de poignée de main.</summary>
    public const int HandshakeOpcode = 0;

    /// <summary>Opération d'échange de commande.</summary>
    public const int FrameOpcode = 1;

    /// <summary>Opération de fermeture, envoyée par Discord en cas d'erreur.</summary>
    public const int CloseOpcode = 2;

    /// <summary>Version du protocole IPC attendue par Discord.</summary>
    public const int ProtocolVersion = 1;

    /// <summary>Encode la poignée de main, qui authentifie seulement LinkLauncher auprès de Discord.</summary>
    /// <param name="applicationId">Identifiant d'application Discord.</param>
    public static byte[] EncodeHandshake(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        var payload = new JsonObject
        {
            ["v"] = ProtocolVersion,
            ["client_id"] = applicationId
        };

        return EncodeFrame(HandshakeOpcode, payload.ToJsonString());
    }

    /// <summary>Encode la publication d'une activité.</summary>
    /// <param name="processId">Identifiant du processus LinkLauncher.</param>
    /// <param name="activity">Activité à publier.</param>
    /// <param name="nonce">Identifiant unique de la demande.</param>
    public static byte[] EncodeSetActivity(int processId, DiscordActivity activity, string nonce)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        var activityPayload = new JsonObject
        {
            ["details"] = activity.Details,
            ["state"] = activity.State,
            ["timestamps"] = new JsonObject
            {
                ["start"] = activity.StartedAt.ToUnixTimeMilliseconds()
            }
        };

        return EncodeCommand(processId, activityPayload, nonce);
    }

    /// <summary>Encode l'effacement de l'activité.</summary>
    /// <param name="processId">Identifiant du processus LinkLauncher.</param>
    /// <param name="nonce">Identifiant unique de la demande.</param>
    public static byte[] EncodeClearActivity(int processId, string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        return EncodeCommand(processId, activity: null, nonce);
    }

    /// <summary>Décode une trame reçue de Discord.</summary>
    /// <param name="buffer">Tampon de lecture.</param>
    /// <param name="length">Nombre d'octets valides dans le tampon.</param>
    /// <param name="frame">Trame décodée.</param>
    /// <param name="consumed">Nombre d'octets consommés.</param>
    public static bool TryReadFrame(byte[] buffer, int length, out DiscordIpcFrame? frame, out int consumed)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        frame = null;
        consumed = 0;

        if (length < 8)
        {
            return false;
        }

        var opcode = BitConverter.ToInt32(buffer, 0);
        var payloadLength = BitConverter.ToInt32(buffer, 4);

        if (payloadLength < 0 || payloadLength > length - 8)
        {
            return false;
        }

        var json = Encoding.UTF8.GetString(buffer, 8, payloadLength);
        frame = new DiscordIpcFrame(opcode, json);
        consumed = 8 + payloadLength;
        return true;
    }

    /// <summary>Extrait le message d'erreur d'une trame de fermeture, pour la journalisation.</summary>
    /// <param name="json">Charge utile reçue.</param>
    public static string DescribeError(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "Fermeture sans détail.";
        }

        try
        {
            var node = JsonNode.Parse(json);
            var code = node?["code"]?.GetValue<int>();
            var message = node?["message"]?.GetValue<string>();

            return message is null
                ? "Fermeture sans détail."
                : code is null
                    ? message
                    : $"{message} (code {code.Value.ToString(CultureInfo.InvariantCulture)})";
        }
        catch (JsonException)
        {
            return "Fermeture avec une charge utile illisible.";
        }
    }

    private static byte[] EncodeCommand(int processId, JsonNode? activity, string nonce)
    {
        var arguments = new JsonObject
        {
            ["pid"] = processId
        };

        if (activity is not null)
        {
            arguments["activity"] = activity;
        }

        var payload = new JsonObject
        {
            ["cmd"] = "SET_ACTIVITY",
            ["args"] = arguments,
            ["nonce"] = nonce
        };

        return EncodeFrame(FrameOpcode, payload.ToJsonString());
    }

    private static byte[] EncodeFrame(int opcode, string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var frame = new byte[8 + payload.Length];

        BitConverter.TryWriteBytes(frame.AsSpan(0, 4), opcode);
        BitConverter.TryWriteBytes(frame.AsSpan(4, 4), payload.Length);
        payload.CopyTo(frame.AsSpan(8));

        return frame;
    }
}
