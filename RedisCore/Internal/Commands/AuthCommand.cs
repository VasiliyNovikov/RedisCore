using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class AuthCommand : VoidCommand
{
    public AuthCommand(string password) 
        : base(CommandNames.Auth, new RedisCharString(password))
    {
    }
}