using System;
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

    private async Task PopulateReaderHandleErrors()
    {
        try
        {
            await PopulateReader(_inputBackend);
            await _inputBackend.CompleteAsync();
        }
        catch (Exception e)
        {
            await _inputBackend.CompleteAsync(e);
        }
    }
        
    private async Task PopulateWriterHandleErrors()
    {
        try
        {
            await PopulateWriter(_outputBackend);
            await _outputBackend.CompleteAsync();
        }
        catch (Exception e)
        {
            await _outputBackend.CompleteAsync(e);
        }
    }

    private void EnsureStartPopulating()
    {
        _populateTask ??= Task.WhenAll(PopulateReaderHandleErrors(),
            PopulateWriterHandleErrors());
    }
}