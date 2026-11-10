using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>
    /// Все таблицы БД через навигационное меню (ТЗ, F2).
    /// Менеджер: просмотр, добавление и изменение (кроме пользователей), без удаления.
    /// Администратор: всё, включая пользователей, роли и удаление записей.
    /// </summary>
    public partial class DatabaseTablesPage : Page
    {
        /// <summary>Пункт меню: таблица БД.</summary>
        private class TableInfo
        {
            public string Title { get; set; }
            public string Name { get; set; }
            public Type EntityType { get; set; }
        }

        /// <summary>Откуда брать список для выпадающего столбца внешнего ключа.</summary>
        private class ForeignKeySource
        {
            public IList Items { get; set; }
            public string DisplayMember { get; set; }
        }

        private static readonly List<TableInfo> AllTables = new List<TableInfo>
        {
            new TableInfo { Title = "Товары", Name = "Products", EntityType = typeof(Products) },
            new TableInfo { Title = "Остатки на складе", Name = "StockItems", EntityType = typeof(StockItems) },
            new TableInfo { Title = "Категории", Name = "Categories", EntityType = typeof(Categories) },
            new TableInfo { Title = "Подкатегории", Name = "Subcategories", EntityType = typeof(Subcategories) },
            new TableInfo { Title = "Производители", Name = "Manufacturers", EntityType = typeof(Manufacturers) },
            new TableInfo { Title = "Поставщики", Name = "Suppliers", EntityType = typeof(Suppliers) },
            new TableInfo { Title = "Цвета", Name = "Colors", EntityType = typeof(Colors) },
            new TableInfo { Title = "Заказы", Name = "Orders", EntityType = typeof(Orders) },
            new TableInfo { Title = "Состав заказов", Name = "OrderItems", EntityType = typeof(OrderItems) },
            new TableInfo { Title = "Статусы заказа", Name = "OrderStatuses", EntityType = typeof(OrderStatuses) },
            new TableInfo { Title = "Пункты выдачи", Name = "PickupPoints", EntityType = typeof(PickupPoints) },
            new TableInfo { Title = "Пользователи", Name = "Users", EntityType = typeof(Users) },
            new TableInfo { Title = "Роли", Name = "Roles", EntityType = typeof(Roles) },
        };

        /// <summary>Столбцы с типом DATE — редактируются через DatePicker.</summary>
        private static readonly HashSet<string> DateColumns = new HashSet<string> { "RegistrationDate", "DeliveryDate" };

        /// <summary>Столбцы только для чтения.</summary>
        private static readonly HashSet<string> ReadOnlyColumns = new HashSet<string> { "Id", "PasswordHash" };

        private TableInfo _current;
        private HashSet<string> _scalarProperties = new HashSet<string>();
        private Dictionary<string, ForeignKeySource> _foreignKeys = new Dictionary<string, ForeignKeySource>();
        private bool _switching;

        private static TechnoMirEntities Db => AppConnect.model0db;
        private static int? Role => AppConnect.RoleId;

        public DatabaseTablesPage()
        {
            InitializeComponent();

            if (!AccessRules.CanViewAllTables(Role))
            {
                Loaded += (s, e) => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
                return;
            }

            lbTables.ItemsSource = AllTables.Where(t => AccessRules.CanOpenTable(Role, t.Name)).ToList();
            btnDeleteRecord.Visibility = AccessRules.CanDeleteRecords(Role) ? Visibility.Visible : Visibility.Collapsed;
            lbTables.SelectedIndex = 0;
        }

        // ------------------------------------------------------------------ загрузка таблицы

        private void lbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_switching || !(lbTables.SelectedItem is TableInfo table)) return;

            if (Db.ChangeTracker.HasChanges())
            {
                if (!ErrorHandler.Confirm("Есть несохранённые изменения. Отменить их и перейти к другой таблице?"))
                {
                    _switching = true;
                    lbTables.SelectedItem = _current;
                    _switching = false;
                    return;
                }
                AppConnect.RollbackChanges();
            }

            ShowTable(table);
        }

        private void ShowTable(TableInfo table)
        {
            _current = table;
            _scalarProperties = GetScalarProperties(table.EntityType);
            _foreignKeys = LoadForeignKeySources();

            var set = Db.Set(table.EntityType);
            set.Load();

            bool canEdit = AccessRules.CanEditRecords(Role) && AccessRules.CanOpenTable(Role, table.Name);
            dgTable.IsReadOnly = !canEdit;
            dgTable.CanUserAddRows = canEdit;
            dgTable.ItemsSource = null;
            dgTable.Columns.Clear();
            dgTable.ItemsSource = set.Local;   // DataGrid правит записи прямо в контексте EF

            btnResetPassword.Visibility = table.Name == "Users" && AccessRules.CanManageUsers(Role)
                ? Visibility.Visible : Visibility.Collapsed;

            txtHint.Text = AccessRules.CanDeleteRecords(Role)
                ? "Изменяйте ячейки, новая запись — в пустой строке внизу. Затем «Сохранить изменения». " +
                  "Для удаления выделите строки и нажмите «Удалить запись»."
                : "Изменяйте ячейки, новая запись — в пустой строке внизу. Затем «Сохранить изменения». " +
                  "Удаление записей доступно только администратору.";
            if (table.Name == "Users")
                txtHint.Text += $" Новый пользователь получает пароль {EntityRules.DefaultPassword}.";
        }

        /// <summary>Только настоящие столбцы таблицы (из модели EF), без навигационных и вычисляемых свойств.</summary>
        private static HashSet<string> GetScalarProperties(Type type)
        {
            var context = ((IObjectContextAdapter)Db).ObjectContext;
            var entityType = context.MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace)
                .First(t => t.Name == type.Name);
            return new HashSet<string>(entityType.Properties.Select(p => p.Name));
        }

        /// <summary>Списки для выпадающих столбцов внешних ключей: вместо кода показываем название.</summary>
        private static Dictionary<string, ForeignKeySource> LoadForeignKeySources()
        {
            ForeignKeySource Src(IList items, string display) => new ForeignKeySource { Items = items, DisplayMember = display };

            return new Dictionary<string, ForeignKeySource>
            {
                ["RoleId"] = Src(Db.Roles.OrderBy(x => x.Id).ToList(), "RoleName"),
                ["CategoryId"] = Src(Db.Categories.OrderBy(x => x.CategoryName).ToList(), "CategoryName"),
                ["SubcategoryId"] = Src(Db.Subcategories.ToList().OrderBy(x => x.FullName).ToList(), "FullName"),
                ["ManufacturerId"] = Src(Db.Manufacturers.OrderBy(x => x.ManufacturerName).ToList(), "ManufacturerName"),
                ["SupplierId"] = Src(Db.Suppliers.OrderBy(x => x.SupplierName).ToList(), "SupplierName"),
                ["ColorId"] = Src(Db.Colors.OrderBy(x => x.ColorName).ToList(), "ColorName"),
                ["ProductId"] = Src(Db.Products.OrderBy(x => x.Name).ToList(), "Name"),
                ["UserId"] = Src(Db.Users.OrderBy(x => x.Surname).ToList(), "FIO"),
                ["PickupPointId"] = Src(Db.PickupPoints.OrderBy(x => x.Address).ToList(), "Address"),
                ["StatusId"] = Src(Db.OrderStatuses.OrderBy(x => x.Id).ToList(), "StatusName"),
                ["OrderId"] = Src(Db.Orders.OrderBy(x => x.OrderNumber).ToList(), "OrderNumber"),
                ["StockItemId"] = Src(Db.StockItems.ToList().OrderBy(x => x.DisplayName).ToList(), "DisplayName"),
            };
        }

        private void dgTable_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string name = e.PropertyName;

            // Навигационные свойства (Roles, OrderItems…) и вычисляемые (FIO, FinalPrice…) не показываем
            if (!_scalarProperties.Contains(name))
            {
                e.Cancel = true;
                return;
            }

            if (_foreignKeys.TryGetValue(name, out var fk))
            {
                e.Column = new DataGridComboBoxColumn
                {
                    Header = name,
                    ItemsSource = fk.Items,
                    DisplayMemberPath = fk.DisplayMember,
                    SelectedValuePath = "Id",
                    SelectedValueBinding = new Binding(name) { Mode = BindingMode.TwoWay }
                };
            }
            else if (DateColumns.Contains(name))
            {
                var picker = new FrameworkElementFactory(typeof(DatePicker));
                picker.SetBinding(DatePicker.SelectedDateProperty,
                    new Binding(name) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                picker.SetValue(IsEnabledProperty, !dgTable.IsReadOnly);
                e.Column = new DataGridTemplateColumn
                {
                    Header = name,
                    CellTemplate = new DataTemplate { VisualTree = picker },
                    SortMemberPath = name
                };
            }

            e.Column.Header = name;
            if (ReadOnlyColumns.Contains(name))
                e.Column.IsReadOnly = true;
        }

        /// <summary>Новая строка в таблице: заполняем значения по умолчанию.</summary>
        private void dgTable_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
            => EntityRules.FillDefaults(e.NewItem);

        // ------------------------------------------------------------------ кнопки

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            dgTable.CommitEdit(DataGridEditingUnit.Row, true);

            var changed = Db.ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
                .ToList();
            if (changed.Count == 0)
            {
                ErrorHandler.ShowInfo("Изменений нет.");
                return;
            }

            // Проверяем каждую изменённую запись до сохранения
            var errors = new List<string>();
            foreach (var entry in changed)
            {
                if (entry.State == EntityState.Added)
                    EntityRules.FillDefaults(entry.Entity);
                foreach (string error in EntityRules.Validate(entry.Entity))
                    errors.Add($"Запись Id = {GetId(entry.Entity)}: {error}");
            }
            if (errors.Count > 0)
            {
                ErrorHandler.ShowWarning(string.Join("\n", errors.Take(15)));
                return;
            }

            try
            {
                Db.SaveChanges();
                dgTable.Items.Refresh();
                ErrorHandler.ShowInfo($"Сохранено записей: {changed.Count}.", "Готово");
            }
            catch (Exception ex)
            {
                // Изменения не откатываем, чтобы их можно было исправить и сохранить ещё раз
                ErrorHandler.Show(ex, "Ошибка сохранения");
            }
        }

        private void btnCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            dgTable.CancelEdit(DataGridEditingUnit.Row);
            if (!Db.ChangeTracker.HasChanges()) return;
            if (!ErrorHandler.Confirm("Отменить все несохранённые изменения?")) return;
            AppConnect.RollbackChanges();
            ShowTable(_current);
        }

        private void btnDeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessRules.CanDeleteRecords(Role))
            {
                ErrorHandler.ShowWarning("Удалять записи может только администратор.", "Доступ ограничен");
                return;
            }

            var selected = dgTable.SelectedItems.Cast<object>()
                .Where(x => x != CollectionView.NewItemPlaceholder)
                .ToList();
            if (selected.Count == 0)
            {
                ErrorHandler.ShowInfo("Выделите запись, которую нужно удалить.");
                return;
            }
            if (Db.ChangeTracker.HasChanges())
            {
                ErrorHandler.ShowWarning("Сначала сохраните или отмените текущие изменения.");
                return;
            }

            // Бизнес-ограничения (ТЗ, F4)
            foreach (var item in selected)
            {
                string denied = EntityRules.CheckDelete(item);
                if (denied != null)
                {
                    ErrorHandler.ShowWarning(denied, "Удаление невозможно");
                    return;
                }
            }

            if (!ErrorHandler.Confirm($"Удалить выбранные записи ({selected.Count} шт.)?")) return;

            try
            {
                foreach (var item in selected)
                    EntityRules.Remove(item);
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка удаления");
            }
            ShowTable(_current);
        }

        private void btnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgTable.SelectedItem is Users user))
            {
                ErrorHandler.ShowInfo("Выберите пользователя.");
                return;
            }
            if (!ErrorHandler.Confirm($"Сбросить пароль пользователя {user.FIO} на {EntityRules.DefaultPassword}?")) return;

            try
            {
                user.PasswordHash = PasswordHasher.GetHash(EntityRules.DefaultPassword);
                Db.SaveChanges();
                ErrorHandler.ShowInfo($"Новый пароль: {EntityRules.DefaultPassword}", "Пароль сброшен");
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка");
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Db.ChangeTracker.HasChanges())
            {
                if (!ErrorHandler.Confirm("Есть несохранённые изменения. Отменить их и выйти?")) return;
                AppConnect.RollbackChanges();
            }
            AppFrame.mainFrame.Navigate(new ProductCatalogPage());
        }

        private static object GetId(object entity)
            => entity.GetType().GetProperty("Id")?.GetValue(entity) ?? "?";
    }
}
