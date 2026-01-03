using Riok.Mapperly.Abstractions;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class BarcodeMapper
{
    /// <summary>
    /// Maps a Barcode entity to a BarcodeDto.
    /// Username is not mapped by Mapperly; must be set manually when User is loaded.
    /// </summary>
    [MapperIgnoreSource(nameof(Barcode.User))]
    [MapperIgnoreSource(nameof(Barcode.Scans))]
    [MapperIgnoreTarget(nameof(BarcodeDto.Username))]
    public static partial BarcodeDto ToDto(this Barcode source);

    /// <summary>
    /// Maps a Barcode entity to a BarcodeDto including the Username from User navigation property.
    /// </summary>
    public static BarcodeDto ToDtoWithUser(this Barcode source)
    {
        var dto = source.ToDto();
        dto.Username = source.User?.Username;
        return dto;
    }

    /// <summary>
    /// Maps a list of Barcode entities to a list of BarcodeDto.
    /// </summary>
    public static List<BarcodeDto> ToDtoList(this IEnumerable<Barcode> source)
    {
        return source.Select(b => b.ToDto()).ToList();
    }

    /// <summary>
    /// Maps a list of Barcode entities to a list of BarcodeDto including Username.
    /// </summary>
    public static List<BarcodeDto> ToDtoListWithUser(this IEnumerable<Barcode> source)
    {
        return source.Select(b => b.ToDtoWithUser()).ToList();
    }
}
