using System;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore.Pipelines
{
    internal static class ExtendedPipe
    {
        public static (ExtendedPipeReader, ExtendedPipeWriter) Create(Pipe pipe)
        {
            var events = new ExtendedPipeEvents();
            return (new ExtendedPipeReader(pipe.Reader, events), new ExtendedPipeWriter(pipe.Writer, events));
        }
    }
    
    internal class ExtendedPipeEvents
    {
        public event Action<ExceptionDispatchInfo> OnReaderCompletionError;
        public event Action<ExceptionDispatchInfo> OnWriterCompletionError;

        public void SetReaderCompletionError(Exception e)
        {
            OnReaderCompletionError?.Invoke(ExceptionDispatchInfo.Capture(e));
        }
        
        public void SetWriterCompletionError(Exception e)
        {
            OnWriterCompletionError?.Invoke(ExceptionDispatchInfo.Capture(e));
        }
    }
    
    internal class ExtendedPipeReader : PipeReader
    {
        private readonly PipeReader _reader;
        private readonly ExtendedPipeEvents _events;
        private ExceptionDispatchInfo _writerCompletionError;

        public bool IsCompleted { get; private set; }

        private void CheckCompleted()
        {
            if (IsCompleted)
                throw new PipeCompletedException();
        }

        public ExtendedPipeReader(PipeReader reader, ExtendedPipeEvents events)
        {
            _reader = reader;
            _events = events;
            _events.OnWriterCompletionError += e => _writerCompletionError = e;
        }
        
        public override void AdvanceTo(SequencePosition consumed)
        {
            CheckCompleted();
            _reader.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            CheckCompleted();
            _reader.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            CheckCompleted();
            _reader.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            CheckCompleted();
            IsCompleted = true;
            if (exception != null)
                _events.SetReaderCompletionError(exception);
            _reader.Complete(exception);
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            _reader.OnWriterCompleted(callback, state);
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            CheckCompleted();
            var result = await _reader.ReadAsync(cancellationToken);
            if (result.IsCompleted)
                _writerCompletionError?.Throw();
            return result;
        }

        public override bool TryRead(out ReadResult result)
        {
            CheckCompleted();
            var succeeded = _reader.TryRead(out result);
            if (result.IsCompleted)
                _writerCompletionError?.Throw();
            return succeeded;
        }
    }

    internal class ExtendedPipeWriter : PipeWriter
    {
        private readonly PipeWriter _writer;
        private readonly ExtendedPipeEvents _events;
        private ExceptionDispatchInfo _readerCompletionError;

        public bool IsCompleted { get; private set; }

        private void CheckCompleted()
        {
            if (IsCompleted)
                throw new PipeCompletedException();
        }

        public ExtendedPipeWriter(PipeWriter writer, ExtendedPipeEvents events)
        {
            _writer = writer;
            _events = events;
            _events.OnReaderCompletionError += e => _readerCompletionError = e;
        }

        public override void Advance(int bytes)
        {
            CheckCompleted();
            _writer.Advance(bytes);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckCompleted();
            return _writer.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckCompleted();
            return _writer.GetSpan(sizeHint);
        }

        public override void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            _writer.OnReaderCompleted(callback, state);
        }

        public override void CancelPendingFlush()
        {
            CheckCompleted();
            _writer.CancelPendingFlush();
        }

        public override void Complete(Exception exception = null)
        {
            CheckCompleted();
            IsCompleted = true;
            if (exception != null)
                _events.SetWriterCompletionError(exception);
            _writer.Complete(exception);
        }

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            CheckCompleted();
            var result = await _writer.FlushAsync(cancellationToken);
            if (result.IsCompleted)
                _readerCompletionError?.Throw();
            return result;
        }
    }
}