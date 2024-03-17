namespace ASNParser;

public record Box
{
    public required string SupplierIdentifier { get; init; }
    public required string Identifier { get; init; }

    public required IReadOnlyCollection<Content> Contents { get; set; } 

    public record Content
    {
        public required string PoNumber { get; init; }
        public required string Isbn { get; init; }
        public int Quantity { get; init; }
    }
}