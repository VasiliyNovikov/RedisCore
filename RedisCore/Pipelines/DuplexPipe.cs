using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisCore.Pipelines
{
    public abstract class DuplexPipe : IDuplexPipe
    {
        private readonly ExtendedPipeReader _input;
        private readonly ExtendedPipeWriter _inputBackend;
        private readonly ExtendedPipeWriter _output;
        private readonly ExtendedPipeReader _outputBackend;
        private Task _populateTask;

        protected int BufferSegmentSize { get; }

        public PipeReader Input
        {
            get
            {
                EnsureStartPopulating();
                return _input;
            }
        }

        public PipeWriter Output
        {
            get
            {
                EnsureStartPopulating();
                return _output;
            }
        }

        public bool IsCompleted => _input.IsCompleted || _output.IsCompleted;

        protected DuplexPipe(int bufferSegmentSize = 512, int pauseWriterThreshold = 8192, int resumeWriterThreshold = 4096)
        {
            BufferSegmentSize = bufferSegmentSize;
            var options = new PipeOptions(pauseWriterThreshold: pauseWriterThreshold,
                                          resumeWriterThreshold: resumeWriterThreshold,
                                          minimumSegmentSize: bufferSegmentSize,
                                          useSynchronizationContext: false);
            var inputPipe = new Pipe(options);
            var outputPipe = new Pipe(options);
            (_input, _inputBackend) = ExtendedPipe.Create(inputPipe);
            (_outputBackend, _output) = ExtendedPipe.Create(outputPipe);
        }

        protected abstract Task PopulateReader(PipeWriter readerBackend);
        protected abstract Task PopulateWriter(PipeReader writerBackend);

        private async Task PopulateReaderHandleErrors(PipeWriter readerBackend)
        {
            try
            {
                await PopulateReader(readerBackend);
                readerBackend.Complete();
            }
            catch (Exception e)
            {
                readerBackend.Complete(e);
            }
        }
        
        private async Task PopulateWriterHandleErrors(PipeReader writerBackend)
        {
            try
            {
                await PopulateWriter(writerBackend);
                writerBackend.Complete();
            }
            catch (Exception e)
            {
                writerBackend.Complete(e);
            }
        }

        private void EnsureStartPopulating()
        {
            if (_populateTask == null)
                _populateTask = Task.WhenAll(PopulateReaderHandleErrors(_inputBackend),
                                             PopulateWriterHandleErrors(_outputBackend));
        }
    }
}