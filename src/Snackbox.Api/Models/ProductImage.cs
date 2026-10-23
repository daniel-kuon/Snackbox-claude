namespace Snackbox.Api.Models;

/// <summary>A product's picture for the tablet. Kept in its own table so product lists do not
/// drag the bytes along, and in the database so the tablet needs no internet.</summary>
public class ProductImage
{
    public int ProductId { get; set; }
    public required byte[] Data { get; set; }
    public required string ContentType { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
