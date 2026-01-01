using Microsoft.AspNetCore.Mvc;

namespace Snackbox.Api.Controllers;

[Route("")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get()
    {
        return Redirect("/swagger");
    }
}