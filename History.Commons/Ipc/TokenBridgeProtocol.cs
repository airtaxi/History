using System.Buffers.Binary;
using System.Text.Json;

namespace History.Commons.Ipc;

// Length-prefixed JSON framing shared by the token bridge server and client. One request is sent
// per connection, so the client closes the pipe after reading the response.
internal static class TokenBridgeProtocol
{
    public const string PipeName = "History.WindowsClient.TokenBridge";

    private const int MaxMessageBytes = 64 * 1024;

    public static async Task WriteMessageAsync<T>(Stream stream, T message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var lengthPrefix = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, payload.Length);

        await stream.WriteAsync(lengthPrefix, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<T> ReadMessageAsync<T>(Stream stream, CancellationToken cancellationToken) where T : class
    {
        var lengthPrefix = new byte[sizeof(int)];
        if (!await ReadExactAsync(stream, lengthPrefix, cancellationToken)) return null;

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);
        if (length <= 0 || length > MaxMessageBytes) return null;

        var payload = new byte[length];
        if (!await ReadExactAsync(stream, payload, cancellationToken)) return null;

        return JsonSerializer.Deserialize<T>(payload);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (read == 0) return false;
            offset += read;
        }

        return true;
    }
}
