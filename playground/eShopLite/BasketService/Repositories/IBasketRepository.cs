using BasketService.Models;

namespace BasketService.Repositories;

public interface IBasketRepository
{
    Task<CustomerBasket?> GetBasketAsync(string customerId);
    IEnumerable<string> GetUsers();
    Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string id);
}

