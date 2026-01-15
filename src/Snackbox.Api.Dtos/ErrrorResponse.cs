using System.Text.Json.Serialization;

namespace Snackbox.Api.Dtos;

public class ErrorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
