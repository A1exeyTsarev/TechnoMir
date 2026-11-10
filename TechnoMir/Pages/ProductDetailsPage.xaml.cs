using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>Карточка товара: выбор цвета и количества, добавление в корзину.</summary>
    public partial class ProductDetailsPage : Page
    {
        private readonly Products _product;

        public ProductDetailsPage(Products product)
        {
            InitializeComponent();
            _product = product;
            DataContext = product;

            btnEditProduct.Visibility = AccessRules.CanEditRecords(AppConnect.RoleId)
                ? Visibility.Visible : Visibility.Collapsed;

            if (product.AvailableColors.Count == 0)
            {
                txtNoStock.Visibility = Visibility.Visible;
                btnAddToCart.IsEnabled = false;
            }
            else
            {
                cmbColor.SelectedIndex = 0;
            }
        }

        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            string error = Validator.ValidateSelected(cmbColor.SelectedItem, "цвет");
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                return;
            }
            var stock = (StockItems)cmbColor.SelectedItem;

            error = Validator.ValidateQuantity(txtQuantity.Text, stock.Quantity, out int quantity);
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                txtQuantity.Focus();
                return;
            }

            // В корзине уже может лежать этот товар: вместе не больше остатка
            int inCart = ShoppingCart.Current.GetQuantity(stock.Id);
            error = Validator.ValidateAddToCart(inCart, quantity, stock.Quantity);
            if (error != null)
            {
                ErrorHandler.ShowWarning(error, "Недостаточно товара");
                return;
            }

            ShoppingCart.Current.Add(new CartItem
            {
                StockItemId = stock.Id,
                ProductName = _product.Name,
                ManufacturerName = _product.ManufacturerName,
                ColorName = stock.Colors.ColorName,
                Quantity = quantity,
                PricePerUnit = _product.FinalPrice
            });

            ErrorHandler.ShowInfo($"«{_product.Name}» ({stock.Colors.ColorName}, {quantity} шт.) добавлен в корзину.",
                "Добавлено");
        }

        /// <summary>В поле количества можно вводить только цифры.</summary>
        private void txtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Validator.IsDigitsOnly(e.Text);

        private void btnGoToCart_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new CartPage());

        private void btnEditProduct_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductEditPage(_product));

        private void btnBack_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
    }
}
