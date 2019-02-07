using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class EvalCommand<TResult> : Command<TResult>
    {
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly RedisValueObject[] EmptyArgs = Array.Empty<RedisValueObject>();
        
        private static RedisObject[] CreateArgs(string script, string[] keys, RedisValueObject[] args)
        {
            var result = new RedisObject[args.Length + keys.Length + 2];
            result[0] = script.ToValue();
            result[1] = keys.Length.ToValue();
            for (var i = 0; i < keys.Length; ++i) 
                result[i + 2] = keys[i].ToValue();
            args.CopyTo(result, keys.Length + 2);
            return result;
        }
        
        private EvalCommand(string script, string[] keys, RedisValueObject[] args) 
            : base(CommandNames.Eval, CreateArgs(script, keys ?? EmptyKeys, args ?? EmptyArgs))
        {
        }

        public static EvalCommand<TResult> Create(string script, string[] keys)
        {
            return new EvalCommand<TResult>(script, keys, null);
        }

        public static EvalCommand<TResult> Create<T>(string script, T arg, string[] keys)
        {
            return new EvalCommand<TResult>(script, keys, new[] {arg.ToValue()});
        }

        public static EvalCommand<TResult> Create<T1, T2>(string script, T1 arg1, T2 arg2, string[] keys)
        {
            return new EvalCommand<TResult>(script, keys, new[] {arg1.ToValue(), arg2.ToValue()});
        }

        public static EvalCommand<TResult> Create<T1, T2, T3>(string script, T1 arg1, T2 arg2, T3 arg3, string[] keys)
        {
            return new EvalCommand<TResult>(script, keys, new[] {arg1.ToValue(), arg2.ToValue(), arg3.ToValue()});
        }
    }
}