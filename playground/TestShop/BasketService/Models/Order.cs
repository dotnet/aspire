namespace BasketService.Models;

public class Order
{
    public required string Id { get; set; }
    public string? BuyerId { get; set; }

    public List<BasketItem> Items { get; set; } = new();
}
