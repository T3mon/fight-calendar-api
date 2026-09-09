using FightCalendar.Web.Data;
using FightCalendar.Web.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FightCalendar.Web.Controllers.Api;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(ApplicationDbContext db) : ControllerBase
{
    /// <summary>
    /// Lists every promotion that currently has at least one synced event. Use the <c>code</c> field to filter
    /// <c>/api/events?promotions=...</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> GetPromotions(CancellationToken ct)
    {
        var promotions = await db.Promotions
            .OrderBy(p => p.Name)
            .Select(p => new PromotionDto(p.Id, p.Code, p.Name))
            .ToListAsync(ct);

        return Ok(promotions);
    }
}
