using Npgsql;
using NpgsqlTypes;

namespace ASNParser;

public class FileProcessor : IFileProcessor
{
    private readonly string? _connectionString;
    private readonly ILogger<FileProcessor> _logger;

    public FileProcessor(ILogger<FileProcessor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    
    public async Task ProcessFileAsync(FileSystemEventArgs e, CancellationToken cancellationToken)
    {
        var filePath = e.FullPath;
        var fileReady = await WaitForFile(filePath, cancellationToken);
        if (!fileReady)
        {
            _logger.LogError("Unable to process file {FilePath} because " +
                             "it could not be accessed after multiple attempts", filePath);
            return;
        }
        
        _logger.LogInformation("File created: {FullPath}", e.Name);
        
        const int batchSize = 1000;
        Box? box = null;
        var boxContentsBatch = new List<Box>();
        var contentList = new List<Box.Content>();
        using var reader = new StreamReader(filePath);
        while (await reader.ReadLineAsync(cancellationToken) is {} line)
        {
            if (line.StartsWith("HDR"))
            {
                if (box is not null)
                {
                    box.Contents = contentList.AsReadOnly();
                    boxContentsBatch.Add(box);
                }
                
                var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                box = new Box
                {
                    SupplierIdentifier = parts[1],
                    Identifier = parts[2],
                    Contents = new List<Box.Content>().AsReadOnly()
                };
    
                contentList = new List<Box.Content>();
            }
            else if (line.StartsWith("LINE"))
            {
                var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                contentList.Add(new Box.Content
                {
                    PoNumber = parts[1],
                    Isbn = parts[2],
                    Quantity = int.Parse(parts[3])
                });
            }
            
            if (boxContentsBatch.Count < batchSize) continue;
            
            await InsertIntoTableAsync(boxContentsBatch, cancellationToken);
            boxContentsBatch.Clear();
        }
        
        if (box is null) return;
        box.Contents = contentList.AsReadOnly();
        boxContentsBatch.Add(box);
        await InsertIntoTableAsync(boxContentsBatch, cancellationToken);
    }
    
    private static async Task<bool> WaitForFile(string fullPath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        const int delayBetweenAttempts = 500;
    
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                await Task.Delay(delayBetweenAttempts, cancellationToken);
            }
        }

        return false;
    }
    
    private async Task InsertIntoTableAsync(IEnumerable<Box> boxContents, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var importer = await connection.BeginBinaryImportAsync(
                "COPY box_content_temp (box_supplier_identifier, box_identifier, po_number, isbn, quantity)" +
                "FROM STDIN (FORMAT binary)", cancellationToken))
            {
                foreach (var box in boxContents)
                {
                    foreach (var contentItem in box.Contents)
                    {
                        await importer.StartRowAsync(cancellationToken);
                        await importer.WriteAsync(box.SupplierIdentifier, NpgsqlDbType.Text, cancellationToken);
                        await importer.WriteAsync(box.Identifier, NpgsqlDbType.Text, cancellationToken);
                        await importer.WriteAsync(contentItem.PoNumber, NpgsqlDbType.Text, cancellationToken);
                        await importer.WriteAsync(contentItem.Isbn, NpgsqlDbType.Text, cancellationToken);
                        await importer.WriteAsync(contentItem.Quantity, NpgsqlDbType.Integer, cancellationToken);
                    }
                }
                await importer.CompleteAsync(cancellationToken);
            }
            
            const string transferCommand = @"INSERT INTO box_content (box_supplier_identifier, box_identifier, po_number, isbn, quantity)
                                            SELECT DISTINCT box_supplier_identifier, box_identifier, po_number, isbn, quantity
                                            FROM box_content_temp ON CONFLICT (box_supplier_identifier, box_identifier, po_number, isbn) DO NOTHING;";
            
            await using (var cmd = new NpgsqlCommand(transferCommand, connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await using (var cmd = new NpgsqlCommand("DELETE FROM box_content_temp", connection))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(e, "Error storing data");
            throw;
        }
    }
}