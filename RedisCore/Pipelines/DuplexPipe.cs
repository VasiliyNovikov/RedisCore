using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisCore.Pipelines;

public abstract class DuplexPipe : IDuplexPipe
{
    private readonly PipeReader _input;
    private readonly PipeWriter _inputBackend;
    private readonly PipeWriter _output;
    private readonly PipeReader _outputBackend;
    private Task? _populateTask;

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

    protected DuplexPipe(int bufferSegmentSize = 512, int pauseWriterThreshold = 8192, int resumeWriterThreshold = 4096)
    {
        BufferSegmentSize = bufferSegmentSize;
        var options = new PipeOptions(pauseWriterThreshold: pauseWriterThreshold,
            resumeWriterThreshold: resumeWriterThreshold,
            minimumSegmentSize: bufferSegmentSize,
            useSynchronizationContext: false);
        var inputPipe = new Pipe(options);
        var outputPipe = new Pipe(options);
        _input = inputPipe.Reader;
        _inputBackend = inputPipe.Writer;
        _output = outputPipe.Writer;
        _outputBackend = outputPipe.Reader;
    }

    protected abstract Task PopulateReader(PipeWriter readerBackend);
    protected abstract Task PopulateWriter(PipeReader writerBackend);

    [SuppressMessage("Microsoft.Design", "CA1031: Do not catch general exception types",
                     Justification = "False positive. Exception is asynchronously rethrown")]
    private async Task PopulateReaderHandleErrors()
    {
        try
        {
            await PopulateReader(_inputBackend).ConfigureAwait(false);
            await _inputBackend.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await _inputBackend.CompleteAsync(e).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1031: Do not catch general exception types",
                     Justification = "False positive. Exception is asynchronously rethrown")]
    private async Task PopulateWriterHandleErrors()
    {
        try
        {
            await PopulateWriter(_outputBackend).ConfigureAwait(false);
            await _outputBackend.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await _outputBackend.CompleteAsync(e).ConfigureAwait(false);
        }
    }

    private void EnsureStartPopulating()
    {
        _populateTask ??= Task.WhenAll(PopulateReaderHandleErrors(),
                                       PopulateWriterHandleErrors());
    }
}