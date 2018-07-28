using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisCore.Tests
{
    public static class ExecUtil
    {
        public struct Result
        {
            public Result(int exitCode, string standardOutput = null, string standardError = null)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }
        }
        
        public static async Task<Result> Command(string cmd, string args = "", bool captureStdOut = false, bool captureStdErr = false, string workingDirectory = null)
        {
            var process = new Process {
                StartInfo =
                {
                    FileName = cmd, Arguments = args,
                    RedirectStandardOutput = captureStdOut,
                    RedirectStandardError = captureStdErr,
                    WorkingDirectory = workingDirectory
                },
                EnableRaisingEvents = true};
            var tcs = new TaskCompletionSource<bool>();
            process.Exited += (s, a) => tcs.SetResult(true);
            process.Start();
            await tcs.Task;
            var stdOut = captureStdOut ? await process.StandardOutput.ReadToEndAsync() : null;
            var stdErr = captureStdErr ? await process.StandardError.ReadToEndAsync() : null;
            return new Result(process.ExitCode, stdOut, stdErr);
        }
    }
}