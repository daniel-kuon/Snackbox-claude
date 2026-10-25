namespace Snackbox.Components.Services;

/// <summary>
/// Whether the browser that loaded the page is on this PC (the USB tablet included), as the
/// website saw it on the HTTP request. Only known while that request is prerendered; the tablet
/// page carries the answer over to its interactive circuit in protected persisted state, so
/// navigating to /tablet inside an existing circuit (no HTTP request) cannot skip the check.
/// </summary>
public class TabletClientAccess
{
    public bool? IsLocal { get; set; }
}
