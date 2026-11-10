using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>
    /// Добавление и изменение товара (менеджер, администратор).
    /// Фото выбирается через OpenFileDialog и копируется в ProductImages (ТЗ, F3).
    /// </summary>
    public partial class ProductEditPage : Page
    {
        /// <summary>Строка таблицы остатков на странице (пока не сохранено в БД).</summary>
        private class StockRow
        {
            public Colors Color { get; set; }
            public string ColorName => Color.ColorName;
            public int Quantity { get; set; }
        }

        private readonly Products _product;     // null — новый товар
        private string _imagePath;              // относительный путь для Products.ImagePath
        private readonly List<StockRow> _stock = new List<StockRow>();

        public ProductEditPage(Products product)
        {
            InitializeComponent();

            if (!AccessRules.CanEditRecords(AppConnect.RoleId))
            {
                Loaded += (s, e) => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
                return;
            }

            _product = product;
            var db = AppConnect.model0db;
            cmbCategory.ItemsSource = db.Categories.OrderBy(c => c.CategoryName).ToList();
            cmbManufacturer.ItemsSource = db.Manufacturers.OrderBy(m => m.ManufacturerName).ToList();
            cmbSupplier.ItemsSource = db.Suppliers.OrderBy(s => s.SupplierName).ToList();
            cmbStockColor.ItemsSource = db.Colors.OrderBy(c => c.ColorName).ToList();

            if (product == null)
            {
                txtTitle.Text = "Новый товар";
                txtArticle.Text = SuggestArticle();
            }
            else
            {
                txtTitle.Text = "Изменение товара";
                FillFields(product);
            }
            ShowImage();
            RefreshStock();
        }

        /// <summary>Следующий свободный артикул вида TM-0024.</summary>
        private static string SuggestArticle()
        {
            int next = AppConnect.model0db.Products.Count() + 1;
            string article;
            do { article = $"TM-{next++:0000}"; }
            while (AppConnect.model0db.Products.Any(p => p.Article == article));
            return article;
        }

        private void FillFields(Products p)
        {
            txtArticle.Text = p.Article;
            txtName.Text = p.Name;
            cmbCategory.SelectedItem = p.Subcategories.Categories;
            cmbSubcategory.SelectedItem = p.Subcategories;
            cmbManufacturer.SelectedItem = p.Manufacturers;
            cmbSupplier.SelectedItem = p.Suppliers;
            txtPrice.Text = p.Price.ToString("0.##");
            txtDiscount.Text = p.DiscountPercent?.ToString();
            txtDescription.Text = p.Description;
            txtSpecifications.Text = p.Specifications;
            _imagePath = p.ImagePath;

            foreach (var s in p.StockItems)
                _stock.Add(new StockRow { Color = s.Colors, Quantity = s.Quantity });
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = cmbCategory.SelectedItem as Categories;
            cmbSubcategory.ItemsSource = category?.Subcategories.OrderBy(s => s.SubcategoryName).ToList();
            cmbSubcategory.SelectedItem = null;
        }

        private void ShowImage()
        {
            imgProduct.Source = ImageStorage.Load(_imagePath);
            txtImagePath.Text = string.IsNullOrEmpty(_imagePath) ? "Фото не выбрано" : _imagePath;
        }

        private void btnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите фото товара",
                Filter = ImageStorage.FileFilter
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _imagePath = ImageStorage.SaveImage(dialog.FileName);
                ShowImage();
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "Не удалось скопировать фото");
            }
        }

        private void btnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _imagePath = null;
            ShowImage();
        }

        private void RefreshStock()
        {
            dgStock.ItemsSource = null;
            dgStock.ItemsSource = _stock.OrderBy(s => s.ColorName).ToList();
        }

        private void btnSetStock_Click(object sender, RoutedEventArgs e)
        {
            string error = Validator.ValidateSelected(cmbStockColor.SelectedItem, "цвет")
                           ?? Validator.ValidateStockQuantity(txtStockQuantity.Text, out _);
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                return;
            }
            Validator.ValidateStockQuantity(txtStockQuantity.Text, out int quantity);

            var color = (Colors)cmbStockColor.SelectedItem;
            var row = _stock.FirstOrDefault(s => s.Color.Id == color.Id);
            if (row == null) _stock.Add(new StockRow { Color = color, Quantity = quantity });
            else row.Quantity = quantity;

            txtStockQuantity.Clear();
            RefreshStock();
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            void Check(string error) { if (error != null) errors.Add(error); }

            Check(Validator.ValidateArticle(txtArticle.Text));
            Check(Validator.ValidateRequired(txtName.Text, "Наименование", Validator.ProductNameMaxLength));
            Check(Validator.ValidateSelected(cmbSubcategory.SelectedItem, "подкатегорию"));
            Check(Validator.ValidateSelected(cmbManufacturer.SelectedItem, "производителя"));
            Check(Validator.ValidateSelected(cmbSupplier.SelectedItem, "поставщика"));
            Check(Validator.ValidatePrice(txtPrice.Text, out decimal price));
            Check(Validator.ValidateDiscount(txtDiscount.Text, out int? discount));
            Check(Validator.ValidateOptional(txtDescription.Text, "Описание", Validator.TextMaxLength));
            Check(Validator.ValidateOptional(txtSpecifications.Text, "Характеристики", Validator.TextMaxLength));

            var db = AppConnect.model0db;
            string article = txtArticle.Text.Trim();
            int currentId = _product?.Id ?? 0;
            if (errors.Count == 0 && db.Products.Any(p => p.Article == article && p.Id != currentId))
                errors.Add("Товар с таким артикулом уже есть.");

            if (errors.Count > 0)
            {
                ErrorHandler.ShowWarning(string.Join("\n", errors));
                return;
            }

            var product = _product ?? new Products();
            product.Article = article;
            product.Name = txtName.Text.Trim();
            product.SubcategoryId = ((Subcategories)cmbSubcategory.SelectedItem).Id;
            product.ManufacturerId = ((Manufacturers)cmbManufacturer.SelectedItem).Id;
            product.SupplierId = ((Suppliers)cmbSupplier.SelectedItem).Id;
            product.Price = price;
            product.DiscountPercent = discount;
            product.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            product.Specifications = string.IsNullOrWhiteSpace(txtSpecifications.Text) ? null : txtSpecifications.Text.Trim();
            product.ImagePath = _imagePath;

            try
            {
                if (_product == null)
                    db.Products.Add(product);

                // Остатки: обновляем существующие, добавляем новые
                foreach (var row in _stock)
                {
                    var stock = product.StockItems.FirstOrDefault(s => s.ColorId == row.Color.Id);
                    if (stock == null)
                        db.StockItems.Add(new StockItems { Products = product, ColorId = row.Color.Id, Quantity = row.Quantity });
                    else
                        stock.Quantity = row.Quantity;
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка сохранения товара");
                return;
            }

            ErrorHandler.ShowInfo("Товар сохранён.", "Готово");
            AppFrame.mainFrame.Navigate(new ProductCatalogPage());
        }

        private void Digits_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Validator.IsDigitsOnly(e.Text);

        private void btnCancel_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
    }
}
