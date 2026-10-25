namespace Snackbox.Api.Dtos;

public class TabletProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }

    /// <summary>Compartment 1-9 (never 2, the tablet's); null = not placed, shown below the shelf.</summary>
    public int? ShelfSlot { get; set; }
    public bool HasImage { get; set; }

    /// <summary>Changes with every new picture: part of its address, so a cached old one is never shown.</summary>
    public long ImageVersion { get; set; }
}

public class TabletStateDto
{
    /// <summary>False while nobody has scanned: the tablet just shows the products.</summary>
    public bool Active { get; set; }
    public string? Username { get; set; }
    public List<TabletItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public decimal DiscountedTotal { get; set; }
    public int SecondsLeft { get; set; }
}

public class TabletItemDto
{
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
