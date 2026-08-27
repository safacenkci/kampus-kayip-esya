using KampusKayipEsya.Api.Authorization;
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
    public async Task<ActionResult<IEnumerable<ItemResponse>>> GetItems(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? location,
        [FromQuery] string? status,
        [FromQuery] string? kind,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(kind) && !ItemRules.TryNormalizeKind(kind, out kind))
        {
            return BadRequest(new { error = ItemRules.KindError });
        }

        if (!string.IsNullOrWhiteSpace(status) && !ItemRules.TryNormalizeStatus(status, out status))
        {
            return BadRequest(new { error = ItemRules.StatusError });
        }

        if (!string.IsNullOrWhiteSpace(category) && !ItemRules.TryNormalizeCategory(category, out category))
        {
            return BadRequest(new { error = ItemRules.CategoryError });
        }

        if (!string.IsNullOrWhiteSpace(location) && !ItemRules.TryNormalizeLocation(location, out location))
        {
            return BadRequest(new { error = ItemRules.LocationError });
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
            query = query.Where(i => i.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(i => i.Location == location);
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

        return Ok(ItemMapper.ToListResponse(items));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemResponse>> GetItem(
        int id,
        [FromHeader(Name = ManageToken.HeaderName)] string? manageToken,
        CancellationToken cancellationToken)
    {
        var item = await _db.Items.AsNoTracking()
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var hasValidManageToken = ManageToken.Matches(manageToken, item.ManageTokenHash);
        return Ok(ItemMapper.ToResponse(item, includeHistory: true, isItemDetail: true, hasValidManageToken: hasValidManageToken));
    }

    [HttpGet("{id:int}/matches")]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> GetMatches(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var opposite = ItemRules.OppositeKind(item.Kind);
        var matches = await _db.Items.AsNoTracking()
            .Where(i =>
                i.Id != id &&
                i.Status == ItemRules.StatusOpen &&
                i.Kind == opposite &&
                i.Category == item.Category &&
                i.Location == item.Location)
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        return Ok(ItemMapper.ToListResponse(matches));
    }

    [HttpPost]
    public async Task<ActionResult<ItemResponse>> CreateItem([FromBody] ItemWriteDto dto, CancellationToken cancellationToken)
    {
        if (!TryValidateWrite(dto, out var error, out var kind, out var status, out var location, out var category))
        {
            return BadRequest(new { error });
        }

        var plaintext = ManageToken.Create(out var hash);
        var item = new Item
        {
            Title = dto.Title!.Trim(),
            Description = NormalizeOptional(dto.Description),
            Location = location,
            Category = category,
            Contact = NormalizeOptional(dto.Contact),
            PhotoUrl = NormalizeOptional(dto.PhotoUrl),
            Kind = kind!,
            Status = status ?? ItemRules.StatusOpen,
            CreatedAt = DateTime.UtcNow,
            ManageTokenHash = hash
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetItem),
            new { id = item.Id },
            ItemMapper.ToResponse(item, includeHistory: true, manageToken: plaintext));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemResponse>> UpdateItem(
        int id,
        [FromBody] ItemWriteDto dto,
        [FromHeader(Name = ManageToken.HeaderName)] string? manageToken,
        CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (ForbidIfInvalidManageToken(item, manageToken) is { } forbidden)
        {
            return forbidden;
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
        ApplyStatusChange(item, status ?? item.Status);

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ItemMapper.ToResponse(item, includeHistory: true));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ItemResponse>> UpdateStatus(
        int id,
        [FromBody] StatusUpdateDto dto,
        [FromHeader(Name = ManageToken.HeaderName)] string? manageToken,
        CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (ForbidIfInvalidManageToken(item, manageToken) is { } forbidden)
        {
            return forbidden;
        }

        if (!ItemRules.TryNormalizeStatus(dto.Status, out var status))
        {
            return BadRequest(new { error = ItemRules.StatusError });
        }

        ApplyStatusChange(item, status);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ItemMapper.ToResponse(item, includeHistory: true));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(
        int id,
        [FromHeader(Name = ManageToken.HeaderName)] string? manageToken,
        CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (ForbidIfInvalidManageToken(item, manageToken) is { } forbidden)
        {
            return forbidden;
        }

        _db.Items.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ObjectResult? ForbidIfInvalidManageToken(Item item, string? presented)
    {
        if (ManageToken.Matches(presented, item.ManageTokenHash))
        {
            return null;
        }

        return StatusCode(StatusCodes.Status403Forbidden, new { error = ManageToken.RequiredError });
    }

    private static void ApplyStatusChange(Item item, string status)
    {
        if (item.Status == status)
        {
            return;
        }

        item.StatusHistory.Add(new StatusHistory
        {
            FromStatus = item.Status,
            ToStatus = status,
            ChangedAt = DateTime.UtcNow
        });
        item.Status = status;
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
            error = ItemRules.KindError;
            return false;
        }

        kind = normalizedKind;

        if (!ItemRules.TryNormalizeLocation(dto.Location, out var normalizedLocation))
        {
            error = ItemRules.LocationError;
            return false;
        }

        location = normalizedLocation;

        if (!ItemRules.TryNormalizeCategory(dto.Category, out var normalizedCategory))
        {
            error = ItemRules.CategoryError;
            return false;
        }

        category = normalizedCategory;

        if (dto.Status is not null && dto.Status.Trim().Length > 0)
        {
            if (!ItemRules.TryNormalizeStatus(dto.Status, out var normalizedStatus))
            {
                error = ItemRules.StatusError;
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
