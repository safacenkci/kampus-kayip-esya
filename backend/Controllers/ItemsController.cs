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
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? kind,
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

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryFilter = category.Trim();
            query = query.Where(i => i.Category != null && i.Category.ToLower() == categoryFilter.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            query = query.Where(i => i.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Item>> GetItem(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem([FromBody] ItemWriteDto dto, CancellationToken cancellationToken)
    {
        if (!TryValidateWrite(dto, requireKind: true, out var error, out var kind, out var status))
        {
            return BadRequest(new { error });
        }

        var item = new Item
        {
            Title = dto.Title!.Trim(),
            Description = NormalizeOptional(dto.Description),
            Location = NormalizeOptional(dto.Location),
            Category = NormalizeOptional(dto.Category),
            Contact = NormalizeOptional(dto.Contact),
            PhotoUrl = NormalizeOptional(dto.PhotoUrl),
            Kind = kind!,
            Status = status ?? ItemRules.StatusOpen,
            CreatedAt = DateTime.UtcNow
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Item>> UpdateItem(int id, [FromBody] ItemWriteDto dto, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!TryValidateWrite(dto, requireKind: true, out var error, out var kind, out var status))
        {
            return BadRequest(new { error });
        }

        item.Title = dto.Title!.Trim();
        item.Description = NormalizeOptional(dto.Description);
        item.Location = NormalizeOptional(dto.Location);
        item.Category = NormalizeOptional(dto.Category);
        item.Contact = NormalizeOptional(dto.Contact);
        item.PhotoUrl = NormalizeOptional(dto.PhotoUrl);
        item.Kind = kind!;
        if (status is not null)
        {
            item.Status = status;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(item);
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

        item.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(item);
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
        bool requireKind,
        out string? error,
        out string? kind,
        out string? status)
    {
        error = null;
        kind = null;
        status = null;

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            error = "Title is required.";
            return false;
        }

        if (requireKind)
        {
            if (!ItemRules.TryNormalizeKind(dto.Kind, out var normalizedKind))
            {
                error = "Invalid kind. Allowed values: lost, found.";
                return false;
            }

            kind = normalizedKind;
        }

        if (dto.Status is not null && dto.Status.Trim().Length > 0)
        {
            if (!ItemRules.TryNormalizeStatus(dto.Status, out var normalizedStatus))
            {
                error = "Invalid status. Allowed values: open, claimed, closed.";
                return false;
            }

            status = normalizedStatus;
        }

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
