using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests
{
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
            catch (Exception e)
            {
                pipe.Writer.Complete(e);
            }

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
            catch (Exception e)
            {
                pipe.Reader.Complete(e);
            }

            testData.AsSpan().CopyTo(pipe.Writer.GetSpan(testData.Length));
            pipe.Writer.Advance(testData.Length);

            await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Writer.FlushAsync());
            await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Writer.FlushAsync());
        }

        private class TestException : Exception
        {
        }

        [TestMethod]
        public async Task Pipe_Async_Write_Cancel_ReadWithCancel_Read_Test()
        {
            var currentContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new LockSyncContext());

                var pipe = new Pipe();

                using var cancellationSource = new CancellationTokenSource();

                async Task Producer()
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
            finally
            {
                SynchronizationContext.SetSynchronizationContext(currentContext);
            }
        }

        private class LockSyncContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object state)
            {
                lock (this)
                {
                    base.Post(d, state);
                }
            }
        }
    }
}