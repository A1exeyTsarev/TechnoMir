using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>Корзина и оформление заказа (ТЗ, F2).</summary>
    public partial class CartPage : Page
    {
        private const string NewOrderStatus = "Новый";
        private const int DeliveryDays = 3;

        private readonly ShoppingCart _cart = ShoppingCart.Current;

        public CartPage()
        {
            InitializeComponent();
            cmbPickupPoint.ItemsSource = AppConnect.model0db.PickupPoints.OrderBy(p => p.Address).ToList();
            RefreshCart();
        }

        private void RefreshCart()
        {
            lvCart.ItemsSource = null;
            lvCart.ItemsSource = _cart.Items;
            txtEmpty.Visibility = _cart.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTotal.Text = $"Итого: {_cart.TotalSum:N2} ₽";
            btnCheckout.IsEnabled = _cart.Count > 0;
        }

        private void txtQuantity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox tb) || !(tb.DataContext is CartItem item)) return;
            if (tb.Text == item.Quantity.ToString()) return;

            var stock = AppConnect.model0db.StockItems.Find(item.StockItemId);
            string error = Validator.ValidateQuantity(tb.Text, stock?.Quantity ?? 0, out int quantity);
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                tb.Text = item.Quantity.ToString();
                return;
            }

            item.Quantity = quantity;
            RefreshCart();
        }

        /// <summary>В поле количества можно вводить только цифры.</summary>
        private void txtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Validator.IsDigitsOnly(e.Text);

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is CartItem item)
            {
                _cart.Remove(item);
                RefreshCart();
            }
        }

        private void btnClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) return;
            if (!ErrorHandler.Confirm("Очистить корзину?")) return;
            _cart.Clear();
            RefreshCart();
        }

        private void btnCheckout_Click(object sender, RoutedEventArgs e)
        {
            string error = Validator.ValidateCartNotEmpty(_cart.Count)
                           ?? Validator.ValidateSelected(cmbPickupPoint.SelectedItem, "пункт выдачи");
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                return;
            }

            var db = AppConnect.model0db;

            // Сначала проверяем остатки, чтобы не создать заказ, который нельзя собрать
            foreach (var ci in _cart.Items)
            {
                var stock = db.StockItems.Find(ci.StockItemId);
                if (stock == null || stock.Quantity < ci.Quantity)
                {
                    ErrorHandler.ShowWarning(
                        $"Недостаточно товара «{ci.ProductName}» цвета «{ci.ColorName}». " +
                        $"Доступно: {stock?.Quantity ?? 0} шт.", "Недостаточно товара");
                    return;
                }
            }

            var status = db.OrderStatuses.FirstOrDefault(s => s.StatusName == NewOrderStatus);
            if (status == null)
            {
                ErrorHandler.ShowWarning($"В таблице OrderStatuses нет статуса «{NewOrderStatus}».");
                return;
            }

            var point = (PickupPoints)cmbPickupPoint.SelectedItem;
            int nextNumber = (db.Orders.Any() ? db.Orders.Max(o => o.OrderNumber) : 0) + 1;

            var order = new Orders
            {
                OrderNumber = nextNumber,
                OrderDate = DateTime.Now,
                DeliveryDate = DateTime.Today.AddDays(DeliveryDays),
                UserId = AppConnect.user.Id,
                PickupPointId = point.Id,
                StatusId = status.Id,
                PickupCode = new Random().Next(100, 1000)
            };

            try
            {
                db.Orders.Add(order);
                foreach (var ci in _cart.Items)
                {
                    db.OrderItems.Add(new OrderItems
                    {
                        Orders = order,
                        StockItemId = ci.StockItemId,
                        Quantity = ci.Quantity,
                        PricePerUnit = ci.PricePerUnit
                    });
                    db.StockItems.Find(ci.StockItemId).Quantity -= ci.Quantity;
                }
                db.SaveChanges();   // всё одним сохранением: или весь заказ, или ничего
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка оформления заказа");
                return;
            }

            _cart.Clear();
            ErrorHandler.ShowInfo(
                $"Заказ №{order.OrderNumber} оформлен.\n" +
                $"Пункт выдачи: {point.Address}\n" +
                $"Дата доставки: {order.DeliveryDate:dd.MM.yyyy}\n" +
                $"Код получения: {order.PickupCode}", "Заказ оформлен");
            AppFrame.mainFrame.Navigate(new OrderHistoryPage());
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
    }
}
