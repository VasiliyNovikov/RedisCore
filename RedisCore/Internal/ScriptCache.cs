using System.Collections.Concurrent;
using System.Threading.Tasks;
using RedisCore.Internal.Commands;

namespace RedisCore.Internal;

internal sealed class ScriptCache(RedisClient client)
{
    private readonly ConcurrentDictionary<string, string> _scripts = new();

    /// <summary>
    /// Returns Script's SHA1 hash which can be used to execute it via EVALSHA. Also adds it to local in-memory cache 
    /// </summary>
    public async ValueTask<string> Get(string script)
    {
        if (!_scripts.TryGetValue(script, out var scriptHash))
            _scripts[script] = scriptHash = await GetNoCache(script);
        return scriptHash;
    }

    /// <summary>
    /// Makes sure that all scripts in local in-memory cache available in Redis database. Re-uploads all missing scripts 
    /// </summary>
    public async ValueTask ReUploadAll()
    {
        foreach (var script in _scripts.Keys)
            await GetNoCache(script);
    }

    /// <summary>
    /// Returns Script's SHA1 hash which can be used to execute it via EVALSHA. Doesn't involve local in-memory cache
    /// </summary>
    private async ValueTask<string> GetNoCache(string script) => await client.Execute(new ScriptLoadCommand(script));
}