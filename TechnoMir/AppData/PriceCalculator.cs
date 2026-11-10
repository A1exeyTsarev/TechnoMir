using System;
using System.Collections.Generic;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Расчёты цен. Старая цена — Products.Price, скидка — Products.DiscountPercent,
    /// новая цена считается здесь и в базе не хранится (иначе нарушится 3НФ).
    /// </summary>
    public static class PriceCalculator
    {
        /// <summary>Цена, начиная с которой товар выделяется в каталоге (ТЗ, F3).</summary>
        public const decimal ExpensiveThreshold = 1000m;

        /// <summary>Цена со скидкой, округлённая до копеек. Без скидки — исходная цена.</summary>
        public static decimal CalculateFinalPrice(decimal price, int? discountPercent)
        {
            if (discountPercent == null || discountPercent <= 0)
                return price;
            if (discountPercent >= 100)
                throw new ArgumentOutOfRangeException(nameof(discountPercent), "Скидка должна быть меньше 100 %.");

            return Math.Round(price * (100 - discountPercent.Value) / 100m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>Есть ли у товара скидка.</summary>
        public static bool HasDiscount(int? discountPercent)
            => discountPercent != null && discountPercent > 0;

        /// <summary>Нужно ли выделить товар как дорогой: цена свыше 1000.</summary>
        public static bool IsExpensive(decimal price)
            => price > ExpensiveThreshold;

        /// <summary>Сумма по позициям корзины.</summary>
        public static decimal CalculateTotalPrice(IEnumerable<CartItem> items)
        {
            decimal total = 0;
            if (items == null) return total;
            foreach (var item in items)
                total += item.Sum;
            return total;
        }
    }
}
