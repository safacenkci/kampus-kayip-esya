using KampusKayipEsya.Api.Data;
using KampusKayipEsya.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KampusKayipEsya.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems(
        [FromQuery] string? q,
        [FromQuery] string? kind,
        [FromQuery] string? category,
        [FromQuery] string? location,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(kind) && !ItemRules.TryNormalizeKind(kind, out kind))
        {
            return BadRequest(new { error = "Invalid kind. Allowed values: lost, found." });
        }

        if (!string.IsNullOrWhiteSpace(status) && !ItemRules.TryNormalizeStatus(status, out status))
        {
            return BadRequest(new { error = "Invalid status. Allowed values: open, claimed, closed." });
        }

        IQueryable<Item> query = _db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{EscapeLike(q.Trim())}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Title, pattern, "\\") ||
                (i.Description != null && EF.Functions.ILike(i.Description, pattern, "\\")) ||
                (i.Location != null && EF.Functions.ILike(i.Location, pattern, "\\")));
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(i => i.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryFilter = category.Trim();
            query = query.Where(i => i.Category != null && i.Category.ToLower() == categoryFilter.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationFilter = location.Trim();
            query = query.Where(i => i.Location != null && i.Location.ToLower() == locationFilter.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        return Ok(items.Select(ItemRules.ForClient));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Item>> GetItem(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(ItemRules.ForClient(item));
    }

    [HttpGet("{id:int}/matches")]
    public async Task<ActionResult<IEnumerable<Item>>> GetMatches(int id, CancellationToken cancellationToken)
    {
        var source = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (source is null)
        {
            return NotFound();
        }

        var oppositeKind = source.Kind == ItemRules.KindLost ? ItemRules.KindFound : ItemRules.KindLost;

        var matches = await _db.Items.AsNoTracking()
            .Where(i =>
                i.Id != source.Id &&
                i.Kind == oppositeKind &&
                i.Status == ItemRules.StatusOpen &&
                i.Category != null &&
                source.Category != null &&
                i.Category.ToLower() == source.Category.ToLower() &&
                i.Location != null &&
                source.Location != null &&
                i.Location.ToLower() == source.Location.ToLower())
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        return Ok(matches.Select(ItemRules.ForClient));
    }

    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem([FromBody] ItemWriteDto dto, CancellationToken cancellationToken)
    {
        if (!TryValidateWrite(dto, out var error, out var kind, out var status, out var location, out var category))
        {
            return BadRequest(new { error });
        }

        var now = DateTime.UtcNow;
        var item = new Item
        {
            Title = dto.Title!.Trim(),
            Description = NormalizeOptional(dto.Description),
            Location = location,
            Category = category,
            Contact = NormalizeOptional(dto.Contact),
            PhotoUrl = NormalizeOptional(dto.PhotoUrl),
            Kind = kind!,
            CreatedAt = now
        };

        ItemRules.RecordStatus(item, ItemRules.StatusOpen, now);
        if (status is not null && status != ItemRules.StatusOpen)
        {
            ItemRules.RecordStatus(item, status, now);
        }

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, ItemRules.ForClient(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Item>> UpdateItem(int id, [FromBody] ItemWriteDto dto, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!TryValidateWrite(dto, out var error, out var kind, out var status, out var location, out var category))
        {
            return BadRequest(new { error });
        }

        item.Title = dto.Title!.Trim();
        item.Description = NormalizeOptional(dto.Description);
        item.Location = location;
        item.Category = category;
        item.Contact = NormalizeOptional(dto.Contact);
        item.PhotoUrl = NormalizeOptional(dto.PhotoUrl);
        item.Kind = kind!;
        if (status is not null && status != item.Status)
        {
            ItemRules.RecordStatus(item, status);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ItemRules.ForClient(item));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<Item>> UpdateStatus(int id, [FromBody] StatusUpdateDto dto, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!ItemRules.TryNormalizeStatus(dto.Status, out var status))
        {
            return BadRequest(new { error = "Invalid status. Allowed values: open, claimed, closed." });
        }

        ItemRules.RecordStatus(item, status);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ItemRules.ForClient(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool TryValidateWrite(
        ItemWriteDto dto,
        out string? error,
        out string? kind,
        out string? status,
        out string? location,
        out string? category)
    {
        error = null;
        kind = null;
        status = null;
        location = null;
        category = null;

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            error = "Title is required.";
            return false;
        }

        if (!ItemRules.TryNormalizeKind(dto.Kind, out var normalizedKind))
        {
            error = "Invalid kind. Allowed values: lost, found.";
            return false;
        }

        kind = normalizedKind;

        if (dto.Status is not null && dto.Status.Trim().Length > 0)
        {
            if (!ItemRules.TryNormalizeStatus(dto.Status, out var normalizedStatus))
            {
                error = "Invalid status. Allowed values: open, claimed, closed.";
                return false;
            }

            status = normalizedStatus;
        }

        if (!ItemRules.TryNormalizeLocation(dto.Location, out var normalizedLocation))
        {
            error = "Invalid location. Allowed values: " + string.Join(", ", ItemRules.Locations) + ".";
            return false;
        }

        location = normalizedLocation;

        if (!ItemRules.TryNormalizeCategory(dto.Category, out var normalizedCategory))
        {
            error = "Invalid category. Allowed values: " + string.Join(", ", ItemRules.Categories) + ".";
            return false;
        }

        category = normalizedCategory;

        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
