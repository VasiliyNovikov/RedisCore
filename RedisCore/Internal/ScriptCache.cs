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
                _scripts[script] = scriptHash = await GetNoCache(script);
            return scriptHash;
        }

        public async ValueTask Invalidate()
        {
            foreach (var script in _scripts.Keys) 
                await GetNoCache(script);
        }

        private async ValueTask<string> GetNoCache(string script) => await _client.Execute(new ScriptLoadCommand(script));
    }
}