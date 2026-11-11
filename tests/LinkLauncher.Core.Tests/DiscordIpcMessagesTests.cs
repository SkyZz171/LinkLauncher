namespace LinkLauncher.Core.Tests;

using System.Text;
using System.Text.Json;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class DiscordIpcMessagesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void EncodeHandshake_UsesHandshakeOpcodeAndPublicApplicationId()
    {
        var frame = DiscordIpcMessages.EncodeHandshake("123456789012345678");

        Assert.Equal(DiscordIpcMessages.HandshakeOpcode, BitConverter.ToInt32(frame, 0));
        Assert.Equal(frame.Length - 8, BitConverter.ToInt32(frame, 4));

        var json = Encoding.UTF8.GetString(frame, 8, frame.Length - 8);
        Assert.Contains("\"client_id\":\"123456789012345678\"", json, StringComparison.Ordinal);
        Assert.Contains("\"v\":1", json, StringComparison.Ordinal);
    }

    [Fact]
    public void EncodeSetActivity_CarriesActivityAndProcessId()
    {
        var activity = new DiscordActivity { Details = "Client Roblox lancé", State = "Sans expérience", StartedAt = Now };

        var frame = DiscordIpcMessages.EncodeSetActivity(4242, activity, "nonce-1");

        Assert.Equal(DiscordIpcMessages.FrameOpcode, BitConverter.ToInt32(frame, 0));

        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(frame, 8, frame.Length - 8));
        var root = document.RootElement;

        Assert.Equal("SET_ACTIVITY", root.GetProperty("cmd").GetString());
        Assert.Equal("nonce-1", root.GetProperty("nonce").GetString());

        var arguments = root.GetProperty("args");
        Assert.Equal(4242, arguments.GetProperty("pid").GetInt32());
        Assert.Equal("Client Roblox lancé", arguments.GetProperty("activity").GetProperty("details").GetString());
        Assert.Equal(Now.ToUnixTimeMilliseconds(), arguments.GetProperty("activity").GetProperty("timestamps").GetProperty("start").GetInt64());
    }

    [Fact]
    public void EncodeClearActivity_OmitsActivityPayload()
    {
        var frame = DiscordIpcMessages.EncodeClearActivity(4242, "nonce-2");

        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(frame, 8, frame.Length - 8));
        var arguments = document.RootElement.GetProperty("args");

        Assert.Equal(4242, arguments.GetProperty("pid").GetInt32());
        Assert.False(arguments.TryGetProperty("activity", out _));
    }

    [Fact]
    public void TryReadFrame_RoundTripsEncodedFrame()
    {
        var frame = DiscordIpcMessages.EncodeHandshake("123456789012345678");

        Assert.True(DiscordIpcMessages.TryReadFrame(frame, frame.Length, out var decoded, out var consumed));

        Assert.NotNull(decoded);
        Assert.Equal(DiscordIpcMessages.HandshakeOpcode, decoded!.Opcode);
        Assert.Contains("client_id", decoded.Json, StringComparison.Ordinal);
        Assert.Equal(frame.Length, consumed);
    }

    [Fact]
    public void TryReadFrame_WithTruncatedBuffer_Fails()
    {
        var frame = DiscordIpcMessages.EncodeHandshake("123456789012345678");

        Assert.False(DiscordIpcMessages.TryReadFrame(frame, 4, out var decoded, out var consumed));
        Assert.Null(decoded);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void DescribeError_ReadsCodeAndMessage()
    {
        var description = DiscordIpcMessages.DescribeError("""{"code":4000,"message":"Invalid client id"}""");

        Assert.Contains("Invalid client id", description, StringComparison.Ordinal);
        Assert.Contains("4000", description, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeError_WithUnreadablePayload_StaysNeutral()
    {
        Assert.Equal("Fermeture avec une charge utile illisible.", DiscordIpcMessages.DescribeError("{pas du json"));
        Assert.Equal("Fermeture sans détail.", DiscordIpcMessages.DescribeError(string.Empty));
    }
}
