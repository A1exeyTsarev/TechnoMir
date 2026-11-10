using System.Collections.Generic;
using System.Linq;

namespace TechnoMir.AppData
{
    /// <summary>Позиция корзины: конкретный товар конкретного цвета (StockItems).</summary>
    public class CartItem
    {
        public int StockItemId { get; set; }
        public string ProductName { get; set; }
        public string ManufacturerName { get; set; }
        public string ColorName { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Sum => Quantity * PricePerUnit;
    }

    /// <summary>
    /// Корзина хранится в памяти, пока пользователь работает с программой.
    /// В базу попадает только оформленный заказ (Orders + OrderItems),
    /// поэтому в схеме БД нет отдельной таблицы корзины и нет цикла связей.
    /// </summary>
    public class ShoppingCart
    {
        /// <summary>Корзина текущего пользователя.</summary>
        public static ShoppingCart Current { get; } = new ShoppingCart();

        public List<CartItem> Items { get; } = new List<CartItem>();

        public int Count => Items.Count;

        public decimal TotalSum => PriceCalculator.CalculateTotalPrice(Items);

        /// <summary>Сколько штук этой позиции склада уже в корзине.</summary>
        public int GetQuantity(int stockItemId)
            => Items.Where(i => i.StockItemId == stockItemId).Sum(i => i.Quantity);

        /// <summary>Добавить товар. Если такая позиция уже есть — увеличить количество.</summary>
        public void Add(CartItem item)
        {
            var existing = Items.FirstOrDefault(i => i.StockItemId == item.StockItemId);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                Items.Add(item);
        }

        public void Remove(CartItem item) => Items.Remove(item);

        public void Clear() => Items.Clear();
    }
}
