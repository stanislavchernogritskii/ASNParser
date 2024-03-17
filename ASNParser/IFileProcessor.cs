namespace ASNParser;

public interface IFileProcessor
{
    Task ProcessFileAsync(FileSystemEventArgs e, CancellationToken cancellationToken);
}