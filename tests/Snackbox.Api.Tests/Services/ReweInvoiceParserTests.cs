using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class ReweInvoiceParserTests
{
    private readonly ReweInvoiceParser _parser;

    public ReweInvoiceParserTests()
    {
        _parser = new ReweInvoiceParser();
    }

    [Fact]
    public void Parse_ValidReweInvoice_ExtractsMetadataCorrectly()
    {
        // Arrange
        var invoiceText = @"
R E W E - Center
Friedrich-Ebert-Allee 3-11
22869 Schenefeld
Tel.: 040-83931201
UID Nr.: DE812706034
EUR
POM.LEBERW.FEIN 1,19 B
BIO SALAMI 5,25 B
3 Stk x 1,75
 --------------------------------------
SUMME EUR 34,21
======================================
Geg. Mastercard EUR 34,21

* * Kundenbeleg * *
    Datum: 29.12.2025
    Uhrzeit: 19:16:36 Uhr
    Beleg-Nr. 9862
    Trace-Nr. 010888
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("REWE", result.Metadata.Supplier);
        Assert.Equal("9862", result.Metadata.InvoiceNumber);
        Assert.Equal(new DateTime(2025, 12, 29), result.Metadata.InvoiceDate);
        Assert.Equal(34.21m, result.Metadata.TotalAmount);
    }

    [Fact]
    public void Parse_ValidReweInvoice_ExtractsSimpleItems()
    {
        // Arrange
        var invoiceText = @"
EUR
POM.LEBERW.FEIN 1,19 B
BIO SALAMI 5,25 B
SALAT EISBERG 1,19 B
 --------------------------------------
SUMME EUR 7,63
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Items.Count);
        
        var item1 = result.Items[0];
        Assert.Equal("POM.LEBERW.FEIN", item1.ProductName);
        Assert.Equal(1, item1.Quantity);
        Assert.Equal(1.19m, item1.UnitPrice);
        Assert.Equal(1.19m, item1.TotalPrice);

        var item2 = result.Items[1];
        Assert.Equal("BIO SALAMI", item2.ProductName);
        Assert.Equal(1, item2.Quantity);
        Assert.Equal(5.25m, item2.UnitPrice);
        Assert.Equal(5.25m, item2.TotalPrice);
    }

    [Fact]
    public void Parse_ItemsWithQuantity_ExtractsCorrectly()
    {
        // Arrange
        var invoiceText = @"
EUR
BIO SALAMI 5,25 B
3 Stk x 1,75
BIO KOCHSCHINKEN 8,67 B
3 Stk x 2,89
SALATGURKE 4,47 B
3 Stk x 1,49
 --------------------------------------
SUMME EUR 18,39
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Items.Count);
        
        var item1 = result.Items[0];
        Assert.Equal("BIO SALAMI", item1.ProductName);
        Assert.Equal(3, item1.Quantity);
        Assert.Equal(1.75m, item1.UnitPrice);
        Assert.Equal(5.25m, item1.TotalPrice);

        var item2 = result.Items[1];
        Assert.Equal("BIO KOCHSCHINKEN", item2.ProductName);
        Assert.Equal(3, item2.Quantity);
        Assert.Equal(2.89m, item2.UnitPrice);
        Assert.Equal(8.67m, item2.TotalPrice);

        var item3 = result.Items[2];
        Assert.Equal("SALATGURKE", item3.ProductName);
        Assert.Equal(3, item3.Quantity);
        Assert.Equal(1.49m, item3.UnitPrice);
        Assert.Equal(4.47m, item3.TotalPrice);
    }

    [Fact]
    public void Parse_FullInvoice_ParsesAllItems()
    {
        // Arrange
        var invoiceText = @"
R E W E - Center
Friedrich-Ebert-Allee 3-11
22869 Schenefeld
Tel.: 040-83931201
UID Nr.: DE812706034
EUR
POM.LEBERW.FEIN 1,19 B
BIO SALAMI 5,25 B
3 Stk x 1,75
BIO KOCHSCHINKEN 8,67 B
3 Stk x 2,89
BIO TORTELLONI 2,19 B
SALAT EISBERG 1,19 B
SALATGURKE 4,47 B
3 Stk x 1,49
PAPRIKA BIO 3,39 B
CHAMP.BRAUN BIO 2,49 B
SONNENMAIS JA! 1,99 B
CHIPSF.UNGA. 1,79 B
CHIPS TOM.PAPR. 1,59 B
 --------------------------------------
SUMME EUR 34,21
======================================
Geg. Mastercard EUR 34,21

* * Kundenbeleg * *
    Datum: 29.12.2025
    Uhrzeit: 19:16:36 Uhr
    Beleg-Nr. 9862
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(11, result.Items.Count);
        
        // Verify some items
        Assert.Contains(result.Items, i => i.ProductName == "POM.LEBERW.FEIN" && i.Quantity == 1);
        Assert.Contains(result.Items, i => i.ProductName == "BIO SALAMI" && i.Quantity == 3);
        Assert.Contains(result.Items, i => i.ProductName == "SONNENMAIS JA!" && i.Quantity == 1);
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
    public void Parse_SkipsNonProductLines()
    {
        // Arrange
        var invoiceText = @"
R E W E - Center
EUR
POM.LEBERW.FEIN 1,19 B
 --------------------------------------
SUMME EUR 1,19
======================================
Geg. Mastercard EUR 1,19
Steuer % Netto Steuer Brutto
B= 7,0% 1,11 0,08 1,19
TSE-Signatur: fPYanGgwC5rQTGPLGVRVqv
Bonus-Aktion(en) 0,10 EUR
";

        // Act
        var result = _parser.Parse(invoiceText);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Items);
        Assert.Equal("POM.LEBERW.FEIN", result.Items[0].ProductName);
    }
}
