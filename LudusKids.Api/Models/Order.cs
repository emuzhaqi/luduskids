namespace LudusKids.Api.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];
}
