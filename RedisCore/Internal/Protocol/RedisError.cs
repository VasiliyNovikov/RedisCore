namespace RedisCore.Internal.Protocol;

internal sealed class RedisError(string? type, string message) : RedisObject
{
    public string Type => type ?? "Error";
    public string Message => message;

    public override string ToString() => $"{Type}: {Message}";
}