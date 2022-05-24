using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests;

[TestClass]
public class PipeTests
{
    [TestMethod]
    public async Task PipeReader_Should_Fail_When_PipeWriter_Completed_With_Exception()
    {
        var testData = new byte[64];
        new Random().NextBytes(testData);

        var pipe = new Pipe();

        testData.AsSpan().CopyTo(pipe.Writer.GetSpan(testData.Length));
        pipe.Writer.Advance(testData.Length);
        await pipe.Writer.FlushAsync();

        try
        {
            throw new TestException();
        }
#pragma warning disable CA1031 // Do not catch general exception types - false positive - exception is asynchronously rethrown
        catch (Exception e)
        {
            await pipe.Writer.CompleteAsync(e);
        }
#pragma warning restore CA1031

        await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Reader.ReadAsync());
        await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Reader.ReadAsync());
    }

    [TestMethod]
    public async Task PipeWriter_Should_Fail_When_PipeReader_Completed_With_Exception()
    {
        var testData = new byte[64];
        new Random().NextBytes(testData);

        var pipe = new Pipe();

        try
        {
            throw new TestException();
        }
#pragma warning disable CA1031 // Do not catch general exception types - false positive - exception is asynchronously rethrown
        catch (Exception e)
        {
            await pipe.Reader.CompleteAsync(e);
        }
#pragma warning restore CA1031

        testData.AsSpan().CopyTo(pipe.Writer.GetSpan(testData.Length));
        pipe.Writer.Advance(testData.Length);

        await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Writer.FlushAsync());
        await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Writer.FlushAsync());
    }

    private class TestException : Exception
    {
    }

    [TestMethod]
    public async Task Pipe_Async_Write_Cancel_ReadWithCancel_Read_Test_Repeat_Till_Fail()
    {
        for (var i = 0; i < 10000; ++i)
            await Pipe_Async_Write_Cancel_ReadWithCancel_Read_Test();
    }

    public async Task Pipe_Async_Write_Cancel_ReadWithCancel_Read_Test()
    {
        var pipe = new Pipe();
        using var cancellationSource = new CancellationTokenSource();

        async void Producer()
        {
            await Task.Yield(); // Context switching is needed to repro the issue

            var testData = new byte[64];
            testData.CopyTo(pipe.Writer.GetSpan(testData.Length));
            pipe.Writer.Advance(testData.Length);

            await pipe.Writer.FlushAsync();

            cancellationSource.Cancel(); // Cancel have to be called right after flush to repro the issue
        }

        Producer();
        try
        {
            await pipe.Reader.ReadAsync(cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            await pipe.Reader.ReadAsync();
        }
    }
}