using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public class OutreachEvent
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public OutreachType Type { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(256)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
