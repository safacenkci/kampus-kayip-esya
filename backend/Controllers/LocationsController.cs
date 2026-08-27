using KampusKayipEsya.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace KampusKayipEsya.Api.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<string>> GetLocations()
    {
        return Ok(ItemRules.Locations);
    }
}
