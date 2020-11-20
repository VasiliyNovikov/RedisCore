using System.Collections.Concurrent;
using System.Threading.Tasks;
using RedisCore.Internal.Commands;

namespace RedisCore.Internal
{
    internal class ScriptCache
    {
        private readonly RedisClient _client;
        private readonly ConcurrentDictionary<string, string> _scripts = new ConcurrentDictionary<string, string>();

        public ScriptCache(RedisClient client) => _client = client;

        public async ValueTask<string> Get(string script)
        {
            if (!_scripts.TryGetValue(script, out var scriptHash))
                _scripts[script] = scriptHash = await _client.Execute(new ScriptLoadCommand(script));
            return scriptHash;
        }
    }
}