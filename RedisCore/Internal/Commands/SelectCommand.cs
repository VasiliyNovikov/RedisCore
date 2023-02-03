using System.Collections.Concurrent;
using System.Globalization;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class SelectCommand : VoidCommand
{
    private static readonly ConcurrentDictionary<int, RedisByteString> DatabaseIndices = new();

    private static RedisByteString GetDatabaseValue(int database)
    {
        if (!DatabaseIndices.TryGetValue(database, out var value))
            DatabaseIndices[database] = value = new RedisByteString(database.ToString(CultureInfo.InvariantCulture));
        return value;
    }

    public SelectCommand(int database)
        : base(CommandNames.Select, GetDatabaseValue(database))
    {
    }
}