using System.Text.Json.Serialization;

namespace Snackbox.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShelvingActionType
{
    AddedToStorage,
    AddedToShelf,
    MovedToShelf,
    MovedFromShelf,
    RemovedFromStorage,
    RemovedFromShelf,
    Consumed
}
