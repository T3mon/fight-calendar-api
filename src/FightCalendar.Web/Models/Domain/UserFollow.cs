namespace FightCalendar.Web.Models.Domain;

// A follow is either a whole promotion or a single fighter, never both -
// enforced by a DB check constraint (see ApplicationDbContext.OnModelCreating).
public class UserFollow
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public long? FighterId { get; set; }
    public Fighter? Fighter { get; set; }
}
