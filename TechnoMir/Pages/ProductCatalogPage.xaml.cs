using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>
    /// Каталог товаров (ТЗ, F2–F3): поиск, фильтры, сортировка,
    /// выделение цены свыше 1000, зачёркнутая старая цена при скидке.
    /// </summary>
    public partial class ProductCatalogPage : Page
    {
        private const string AllCategories = "Все категории";
        private const string AllManufacturers = "Все производители";

        private List<Products> _allProducts = new List<Products>();
        private bool _loading;

        public ProductCatalogPage()
        {
            InitializeComponent();
            if (!AccessRules.CanUseShop(AppConnect.RoleId))
            {
                Loaded += (s, e) => AppFrame.mainFrame.Navigate(new AuthorizationPage());
                return;
            }
            ApplyRoleRights();
            LoadData();
        }

        /// <summary>Кнопки по ролям: менеджер — добавление и изменение, администратор — ещё и удаление.</summary>
        private void ApplyRoleRights()
        {
            int? role = AppConnect.RoleId;
            Visibility Show(bool allowed) => allowed ? Visibility.Visible : Visibility.Collapsed;

            btnAddProduct.Visibility = Show(AccessRules.CanEditRecords(role));
            btnEditProduct.Visibility = Show(AccessRules.CanEditRecords(role));
            btnDeleteProduct.Visibility = Show(AccessRules.CanDeleteRecords(role));
            btnDatabaseTables.Visibility = Show(AccessRules.CanViewAllTables(role));
        }

        private void LoadData()
        {
            _loading = true;
            _allProducts = AppConnect.model0db.Products.ToList();

            cmbCategory.Items.Clear();
            cmbCategory.Items.Add(AllCategories);
            foreach (var c in AppConnect.model0db.Categories.OrderBy(c => c.CategoryName))
                cmbCategory.Items.Add(c.CategoryName);
            cmbCategory.SelectedIndex = 0;

            cmbManufacturer.Items.Clear();
            cmbManufacturer.Items.Add(AllManufacturers);
            foreach (var m in AppConnect.model0db.Manufacturers.OrderBy(m => m.ManufacturerName))
                cmbManufacturer.Items.Add(m.ManufacturerName);
            cmbManufacturer.SelectedIndex = 0;

            cmbSort.Items.Clear();
            cmbSort.Items.Add("Без сортировки");
            cmbSort.Items.Add("Цена по возрастанию");
            cmbSort.Items.Add("Цена по убыванию");
            cmbSort.Items.Add("Наименование А–Я");
            cmbSort.SelectedIndex = 0;

            _loading = false;
            ApplyFilters();
            UpdateCartButton();
        }

        private void ApplyFilters()
        {
            if (_loading) return;

            // MaxLength уже не даёт ввести больше 100 символов; это на случай вставки
            if (Validator.ValidateSearch(txtSearch.Text) != null)
                txtSearch.Text = txtSearch.Text.Substring(0, Validator.SearchMaxLength);

            IEnumerable<Products> list = _allProducts;

            string query = txtSearch.Text.Trim().ToLower();
            if (query.Length > 0)
                list = list.Where(p =>
                    p.Name.ToLower().Contains(query) ||
                    p.Article.ToLower().Contains(query) ||
                    (p.Description ?? "").ToLower().Contains(query) ||
                    (p.Specifications ?? "").ToLower().Contains(query));

            if (cmbCategory.SelectedIndex > 0)
            {
                string category = cmbCategory.SelectedItem.ToString();
                list = list.Where(p => p.CategoryName == category);
            }

            if (cmbManufacturer.SelectedIndex > 0)
            {
                string manufacturer = cmbManufacturer.SelectedItem.ToString();
                list = list.Where(p => p.ManufacturerName == manufacturer);
            }

            if (chkDiscountOnly.IsChecked == true)
                list = list.Where(p => p.HasDiscount);

            switch (cmbSort.SelectedIndex)
            {
                case 1: list = list.OrderBy(p => p.FinalPrice); break;
                case 2: list = list.OrderByDescending(p => p.FinalPrice); break;
                case 3: list = list.OrderBy(p => p.Name); break;
            }

            var result = list.ToList();
            lvProducts.ItemsSource = result;
            txtCount.Text = $"Показано {result.Count} из {_allProducts.Count}";
        }

        private void Filters_Changed(object sender, EventArgs e) => ApplyFilters();

        private void UpdateCartButton()
        {
            int count = ShoppingCart.Current.Count;
            btnCart.Content = count > 0 ? $"Корзина ({count})" : "Корзина";
        }

        private Products GetSelectedProduct()
        {
            if (lvProducts.SelectedItem is Products p) return p;
            ErrorHandler.ShowInfo("Сначала выберите товар в списке.");
            return null;
        }

        private void lvProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvProducts.SelectedItem is Products p)
                AppFrame.mainFrame.Navigate(new ProductDetailsPage(p));
        }

        private void btnOpenProduct_Click(object sender, RoutedEventArgs e)
        {
            var p = GetSelectedProduct();
            if (p != null) AppFrame.mainFrame.Navigate(new ProductDetailsPage(p));
        }

        private void btnCart_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new CartPage());

        private void btnOrderHistory_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new OrderHistoryPage());

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductEditPage(null));

        private void btnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            var p = GetSelectedProduct();
            if (p != null) AppFrame.mainFrame.Navigate(new ProductEditPage(p));
        }

        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessRules.CanDeleteRecords(AppConnect.RoleId))
            {
                ErrorHandler.ShowWarning("Удалять товары может только администратор.", "Доступ ограничен");
                return;
            }

            var p = GetSelectedProduct();
            if (p == null) return;

            // Бизнес-ограничение (ТЗ, F4): товар есть в OrderItems — удалять нельзя
            string denied = EntityRules.CheckDelete(p);
            if (denied != null)
            {
                ErrorHandler.ShowWarning(denied, "Удаление невозможно");
                return;
            }

            if (!ErrorHandler.Confirm($"Удалить товар «{p.Name}»?")) return;

            try
            {
                EntityRules.Remove(p);
                AppConnect.model0db.SaveChanges();
                LoadData();
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка удаления");
            }
        }

        private void btnDatabaseTables_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new DatabaseTablesPage());
    }
}
