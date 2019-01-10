using System;
using System.Buffers;
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

            var result = await pipe.Reader.ReadAsync();
            Assert.IsFalse(result.IsCompleted);
            var data = result.Buffer.ToArray();
            pipe.Reader.AdvanceTo(result.Buffer.End);
            CollectionAssert.AreEqual(testData, data);

            try
            {
                throw new TestException();
            }
            catch (Exception e)
            {
                pipe.Writer.Complete(e);
            }

            await Assert.ThrowsExceptionAsync<TestException>(async () => await pipe.Reader.ReadAsync());
        }
        
        [TestMethod]
        public async Task PipeReader_Should_Fail_When_PipeWriter_Completed_With_Exception_2()
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
        }
        
        private class TestException : Exception
        {
        }
    }
}