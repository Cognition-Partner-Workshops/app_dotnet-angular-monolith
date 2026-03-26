using System.ComponentModel.DataAnnotations;

namespace OrderManager.Api.Models;

public class Contact
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ContactUserId { get; set; }
    public User ContactUser { get; set; } = null!;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
