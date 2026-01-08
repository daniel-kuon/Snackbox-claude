using Snackbox.Api.Dtos;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class SonderpostenInvoiceParserTests
{
    private readonly SonderpostenInvoiceParser _parser;

    public SonderpostenInvoiceParserTests()
    {
        _parser = new SonderpostenInvoiceParser();
    }

    [Fact]
    public void Parse_ValidInvoice_ReturnsSuccessWithItems()
    {
        // Arrange
        var invoiceText = @"
Hapex GmbH - Marburger Straße 127 - 35396 Gießen
Belegnummer 1160861
Datum: 21.07.2025, 12:45:24
Rechnung zu Bestell-Nr.: 1345573

Pos. Art-Nr. Bezeichnung Anz. MwSt.Brutto PreisBrutto Gesamt
1 SW25617 M&Ms USA Peanut Butter Chocolate Candies 963,9g MHD:30.7.25 2 7 % 21,00 € 42,00 €
2 SW21346 Lorenz Crunchips WOW Cream & Mild Wasabi 110g MHD:26.8.25 4 7 % 1,11 € 4,44 €
3 SW30416 Mars Secret Centre Biscuits 132g MHD:15.9.25 1 7 % 0,99 € 0,99 €
24 Versand + Verpackungskosten 1 7 % 6,99 € 6,99 €

Gesamtkosten: 114,40 €
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("1160861", result.Metadata.InvoiceNumber);
        Assert.Equal(new DateTime(2025, 7, 21), result.Metadata.InvoiceDate);
        Assert.Equal("Lebensmittel-Sonderposten", result.Metadata.Supplier);

        Assert.Equal(3, result.Items.Count); // Should skip shipping costs

        var firstItem = result.Items[0];
        // Product name should NOT contain MHD date
        Assert.DoesNotContain("MHD:", firstItem.ProductName);
        Assert.Contains("M&Ms USA Peanut Butter", firstItem.ProductName);
        Assert.Equal(2, firstItem.Quantity);
        Assert.Equal(21.00m, firstItem.UnitPrice);
        Assert.Equal(42.00m, firstItem.TotalPrice);
        Assert.Equal("SW25617", firstItem.ArticleNumber);
        Assert.NotNull(firstItem.BestBeforeDate);
        Assert.Equal(new DateTime(2025, 7, 30), firstItem.BestBeforeDate);

        var secondItem = result.Items[1];
        Assert.DoesNotContain("MHD:", secondItem.ProductName);
        Assert.Equal(4, secondItem.Quantity);
        Assert.Equal(1.11m, secondItem.UnitPrice);
        Assert.Equal("SW21346", secondItem.ArticleNumber);
    }

    [Fact]
    public void Parse_EmptyInvoice_ReturnsFailure()
    {
        // Arrange
        var invoiceText = "";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_InvoiceWithoutItems_ReturnsFailure()
    {
        // Arrange
        var invoiceText = @"
Hapex GmbH
Belegnummer 1160861
Datum: 21.07.2025, 12:45:24
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No items found", result.ErrorMessage);
    }

    [Fact]
    public void Parse_SkipsShippingCosts()
    {
        // Arrange
        var invoiceText = @"
Pos. Art-Nr. Bezeichnung Anz. MwSt.Brutto PreisBrutto Gesamt
1 SW12345 Test Product 1 7 % 5,00 € 5,00 €
24 Versand + Verpackungskosten 1 7 % 6,99 € 6,99 €
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
        Assert.DoesNotContain(result.Items, i => i.ProductName.Contains("Versand"));
        Assert.DoesNotContain(result.Items, i => i.ProductName.Contains("Verpackungskosten"));
    }

    [Fact]
    public void Parse_RemovesMhdFromProductName()
    {
        // Arrange
        var invoiceText = @"
Pos. Art-Nr. Bezeichnung Anz. MwSt.Brutto PreisBrutto Gesamt
1 SW12345 Test Product with MHD:30.7.25 date 1 7 % 5,00 € 5,00 €
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.DoesNotContain("MHD:", item.ProductName);
        Assert.Contains("Test Product with", item.ProductName);
        Assert.Contains("date", item.ProductName);
        Assert.NotNull(item.BestBeforeDate);
        Assert.Equal(new DateTime(2025, 7, 30), item.BestBeforeDate);
    }
}
