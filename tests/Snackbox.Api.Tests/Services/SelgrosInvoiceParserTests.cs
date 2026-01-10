using Snackbox.Api.Dtos;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class SelgrosInvoiceParserTests
{
    private readonly SelgrosInvoiceParser _parser;

    public SelgrosInvoiceParserTests()
    {
        _parser = new SelgrosInvoiceParser();
    }

    [Fact]
    public void Parse_ValidInvoice_ReturnsSuccessWithItems()
    {
        // Arrange
        var invoiceText = @"
Belegnummer:
2511435410257757
Belegdatum:
20.12.2025 18:00
Selgros Norderstedt

Pos. GTIN Bezeichnung Menge Inhalt VP Einzelpreis* Warenwert* MwSt
1 4059586509519 SCHWEINEGESCHNETZELTES GYROS 1,145 1 kg 8,400 9,62 7,0 %
2 4059586743548 APFEL BOSKOOP KL.I 70-80 DEUTSCHLAND 1,532 1 kg 2,290 3,51 7,0 %
3 4056479184105 BIO FRISCHMILCH 1,5% HH.1L 2 1 PG M 1,090 2,18 7,0 %
48 4000140718083 DR PEPPER ZERO EW 1,25L 6 1 FL 1,680 10,08 19,0 %
49 879170 DPG 0,25 EUR FLASCHE / DOSE 6 1 FL 0,250 1,50 19,0 %

EUR 370,33
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("2511435410257757", result.Metadata.InvoiceNumber);
        Assert.Equal(new DateTime(2025, 12, 20), result.Metadata.InvoiceDate);
        Assert.Equal("Selgros", result.Metadata.Supplier);

        Assert.Equal(4, result.Items.Count); // Should skip deposit line

        var firstItem = result.Items[0];
        Assert.Contains("SCHWEINEGESCHNETZELTES GYROS", firstItem.ProductName);
        Assert.Equal(1, firstItem.Quantity); // Rounded from 1.145
        Assert.Equal(8.400m, firstItem.UnitPrice);
        Assert.Equal(9.62m, firstItem.TotalPrice);
        Assert.Equal("4059586509519", firstItem.ArticleNumber);

        var secondItem = result.Items[1];
        Assert.Equal(2, secondItem.Quantity); // Rounded from 1.532 kg
        Assert.Equal(2.290m, secondItem.UnitPrice);

        var thirdItem = result.Items[2];
        Assert.Equal(2, thirdItem.Quantity);
        Assert.Equal("4056479184105", thirdItem.ArticleNumber);
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
Belegnummer:
2511435410257757
Belegdatum:
20.12.2025 18:00
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No items found", result.ErrorMessage);
    }

    [Fact]
    public void Parse_SkipsDepositLines()
    {
        // Arrange
        var invoiceText = @"
Pos. GTIN Bezeichnung Menge Inhalt VP Einzelpreis* Warenwert* MwSt
1 4056479184105 BIO FRISCHMILCH 1,5% HH.1L 2 1 PG M 1,090 2,18 7,0 %
49 879170 DPG 0,25 EUR FLASCHE / DOSE 6 1 FL 0,250 1,50 19,0 %
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
        Assert.DoesNotContain(result.Items, i => i.ArticleNumber == "879170");
    }

    [Fact]
    public void Parse_ExtractsTotalAmount()
    {
        // Arrange
        var invoiceText = @"
Belegnummer: 123
Belegdatum: 20.12.2025 18:00
Pos. GTIN Bezeichnung Menge Inhalt VP Einzelpreis* Warenwert* MwSt
1 4056479184105 TEST PRODUCT 1 1 PG 1,000 1,00 7,0 %
EUR 370,33
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(370.33m, result.Metadata.TotalAmount);
    }
}
