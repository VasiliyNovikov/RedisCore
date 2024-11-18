using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class AuthCommand(string password) : VoidCommand(CommandNames.Auth, new RedisCharString(password));