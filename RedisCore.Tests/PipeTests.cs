using System;
using System.IO.Pipelines;
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
    }
}