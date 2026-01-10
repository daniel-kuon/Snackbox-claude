using System.Text.Json.Serialization;

namespace Snackbox.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceItemStatus
{
    Pending,
    Processed
}
