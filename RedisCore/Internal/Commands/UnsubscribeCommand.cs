namespace RedisCore.Internal.Commands
{
    internal class UnsubscribeCommand : VoidCommand
    {
        public UnsubscribeCommand()
            : base(CommandNames.Unsubscribe)
        {
        }
    }
}