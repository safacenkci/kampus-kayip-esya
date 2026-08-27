using KampusKayipEsya.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace KampusKayipEsya.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<string>> GetCategories()
    {
        return Ok(ItemRules.Categories);
    }
}
