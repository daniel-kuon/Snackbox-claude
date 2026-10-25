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

    /// <summary>Done was tapped: the purchase is closed; the tablet shows a summary and *New purchase*.</summary>
    public bool Completed { get; set; }

    /// <summary>Introduction or a dialog open: the countdown stands still.</summary>
    public bool Paused { get; set; }

    /// <summary>The user picks products here; otherwise the tablet shows their card scans.</summary>
    public bool Picking { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }

    /// <summary>Card scans of this purchase, by amount (users who pay with their card codes).</summary>
    public List<TabletCardScanDto> CardScans { get; set; } = new();

    /// <summary>What the user owes now, this purchase included (negative = credit).</summary>
    public decimal OpenAmount { get; set; }

    /// <summary>First time on the tablet: play the introduction.</summary>
    public bool ShowIntro { get; set; }
    public List<TabletItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public decimal DiscountedTotal { get; set; }
    public int SecondsLeft { get; set; }
}

public class TabletCardScanDto
{
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class TabletItemDto
{
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Shown for a few seconds after Done.</summary>
public class TabletSummaryDto
{
    /// <summary>What Done just closed.</summary>
    public List<TabletItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }

    /// <summary>The newest purchases of this month (the closed one included), newest first.</summary>
    public List<TabletPurchaseLineDto> RecentPurchases { get; set; } = new();

    public TabletPaymentLineDto? LastPayment { get; set; }
}

public class TabletPurchaseLineDto
{
    public DateTime At { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class TabletPaymentLineDto
{
    public DateTime At { get; set; }
    public decimal Amount { get; set; }
}
