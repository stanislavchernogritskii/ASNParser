namespace ASNParser;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFileProcessor _fileProcessor;

    public Worker(ILogger<Worker> logger, IFileProcessor fileProcessor)
    {
        _logger = logger;
        _fileProcessor = fileProcessor;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var fileWatcher = InitializeFileWatcher(cancellationToken);
        _logger.LogInformation("Watching for files in {Path}", fileWatcher.Path);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
    
    private FileSystemWatcher InitializeFileWatcher(CancellationToken cancellationToken)
    {
        var watcher = new FileSystemWatcher
        {
            Path = Path.Combine(Directory.GetCurrentDirectory(), "ASNParser"),
            Filter = "*.txt",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        watcher.Created += async (source, e) => 
        {
            await _fileProcessor.ProcessFileAsync(e, cancellationToken);
        };
        watcher.EnableRaisingEvents = true;
        return watcher;
    }
}