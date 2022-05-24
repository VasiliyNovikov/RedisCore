﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace RedisCore.Tests;

public static class ExecUtil
{
    [SuppressMessage("Microsoft.Performance", "CA1815: Override equals and operator equals on value types", Justification = "This type has reference type semantics"),
     SuppressMessage("Microsoft.Design", "CA1034: Nested types should not be visible", Justification = "By design")]
    public struct Result
    {
        public Result(int exitCode, string? standardOutput = null, string? standardError = null)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public int ExitCode { get; }
        public string? StandardOutput { get; }
        public string? StandardError { get; }
    }

    public static async Task<Result> Command(string cmd, string args = "", bool captureStdOut = false, bool captureStdErr = false, string? workingDirectory = null)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName = cmd, Arguments = args,
                RedirectStandardOutput = captureStdOut,
                RedirectStandardError = captureStdErr,
                WorkingDirectory = workingDirectory
            },
            EnableRaisingEvents = true
        };
        var tcs = new TaskCompletionSource<bool>();
        process.Exited += (_, _) => tcs.SetResult(true);
        process.Start();
        await tcs.Task.ConfigureAwait(false);
        var stdOut = captureStdOut ? await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false) : null;
        var stdErr = captureStdErr ? await process.StandardError.ReadToEndAsync().ConfigureAwait(false) : null;
        return new Result(process.ExitCode, stdOut, stdErr);
    }
}