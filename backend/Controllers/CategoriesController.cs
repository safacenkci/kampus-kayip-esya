using KampusKayipEsya.Api.Data;
using KampusKayipEsya.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KampusKayipEsya.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories(CancellationToken cancellationToken)
    {
        var used = await _db.Items.AsNoTracking()
            .Where(i => i.Category != null && i.Category != "")
            .Select(i => i.Category!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var categories = ItemRules.DefaultCategories
            .Concat(used)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(categories);
    }
}
