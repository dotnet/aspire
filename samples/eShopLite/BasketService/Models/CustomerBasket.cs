namespace BasketService.Models;

public class CustomerBasket
{
    public string? BuyerId { get; set; }

    public List<BasketItem> Items { get; set; } = new();

    public int TotalItemCount => Items.Sum(i => i.Quantity);

    public CustomerBasket()
    {
    }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}

