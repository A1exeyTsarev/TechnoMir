using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TechnoMir.AppData
{
    // ============================================================
    //  РАСШИРЕНИЯ СУЩНОСТЕЙ EF (partial-классы)
    //  Сами классы Users, Products и т.д. генерирует модель EF
    //  (AppData/Model1.edmx) в том же пространстве имён.
    // ============================================================

    public partial class Users
    {
        public string FIO => $"{Surname} {FirstName} {Patronymic}".Trim();
        public string RoleName => Roles?.RoleName ?? "";
    }

    public partial class Products
    {
        private static readonly Brush ExpensiveBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xC9)));
        private static readonly Brush NormalBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE3, 0xED, 0xFB)));

        public bool HasDiscount => PriceCalculator.HasDiscount(DiscountPercent);

        /// <summary>Цена со скидкой (или обычная цена, если скидки нет).</summary>
        public decimal FinalPrice => PriceCalculator.CalculateFinalPrice(Price, DiscountPercent);

        /// <summary>Цена свыше 1000 — товар выделяется (ТЗ, F3).</summary>
        public bool IsExpensive => PriceCalculator.IsExpensive(FinalPrice);

        public Brush CardBackground => IsExpensive ? ExpensiveBrush : NormalBrush;
        public FontWeight PriceWeight => IsExpensive ? FontWeights.Bold : FontWeights.Normal;
        public Visibility ExpensiveVisibility => IsExpensive ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OldPriceVisibility => HasDiscount ? Visibility.Visible : Visibility.Collapsed;
        public string DiscountText => HasDiscount ? $"−{DiscountPercent}%" : "";

        public string CategoryName => Subcategories?.Categories?.CategoryName ?? "";
        public string ManufacturerName => Manufacturers?.ManufacturerName ?? "";

        /// <summary>Картинка из ProductImages или заглушка.</summary>
        public ImageSource ImageSource => ImageStorage.Load(ImagePath);

        public int TotalQuantity => StockItems.Sum(s => s.Quantity);
        public string QuantityText => TotalQuantity > 0 ? $"В наличии: {TotalQuantity} шт." : "Нет в наличии";

        /// <summary>Цвета с положительным остатком.</summary>
        public List<StockItems> AvailableColors => StockItems.Where(s => s.Quantity > 0).ToList();

        private static Brush Freeze(Brush b) { b.Freeze(); return b; }
    }

    public partial class StockItems
    {
        public string DisplayName => $"{Products?.Name} — {Colors?.ColorName}";
        public string ColorWithQuantity => $"{Colors?.ColorName} (осталось {Quantity} шт.)";
    }

    public partial class Orders
    {
        public string ClientFIO => Users?.FIO ?? "";
        public string StatusName => OrderStatuses?.StatusName ?? "";
        public string PickupAddress => PickupPoints?.Address ?? "";
        public decimal TotalSum => OrderItems.Sum(i => i.Sum);
        public string Title => $"Заказ №{OrderNumber}";
    }

    public partial class OrderItems
    {
        public decimal Sum => Quantity * PricePerUnit;
        public string ProductName => StockItems?.Products?.Name ?? "";
        public string ColorName => StockItems?.Colors?.ColorName ?? "";
    }

    public partial class Subcategories
    {
        public string FullName => $"{Categories?.CategoryName} / {SubcategoryName}";
    }
}
