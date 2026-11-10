using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Проверка записей, которые менеджер или администратор правит
    /// в разделе «Таблицы БД», и значения по умолчанию для новых записей.
    /// </summary>
    public static class EntityRules
    {
        /// <summary>Пароль, который получает пользователь, созданный администратором.</summary>
        public const string DefaultPassword = "User123";

        /// <summary>Заполняет поля новой записи, которые пользователь обычно не вводит.</summary>
        public static void FillDefaults(object entity)
        {
            var db = AppConnect.model0db;
            switch (entity)
            {
                case Users u:
                    if (string.IsNullOrEmpty(u.PasswordHash)) u.PasswordHash = PasswordHasher.GetHash(DefaultPassword);
                    if (u.RegistrationDate == default(DateTime)) u.RegistrationDate = DateTime.Today;
                    if (u.RoleId == 0) u.RoleId = AccessRules.DefaultRoleId;
                    break;
                case Orders o:
                    if (o.OrderDate == default(DateTime)) o.OrderDate = DateTime.Now;
                    if (o.OrderNumber == 0)
                        o.OrderNumber = (db.Orders.Any() ? db.Orders.Max(x => x.OrderNumber) : 0) + 1;
                    if (o.PickupCode == 0) o.PickupCode = new Random().Next(100, 1000);
                    break;
            }
        }

        /// <summary>Список ошибок записи (пустой — всё верно).</summary>
        public static List<string> Validate(object entity)
        {
            var errors = new List<string>();
            void Add(string e) { if (e != null) errors.Add(e); }

            switch (entity)
            {
                case Roles r:
                    Add(Validator.ValidateRequired(r.RoleName, "Роль", 50));
                    break;
                case Users u:
                    Add(Validator.ValidatePersonName(u.Surname, "Фамилия"));
                    Add(Validator.ValidatePersonName(u.FirstName, "Имя"));
                    Add(Validator.ValidatePersonName(u.Patronymic, "Отчество", false));
                    Add(Validator.ValidateEmail(u.Email));
                    Add(Validator.ValidatePhone(u.Phone));
                    if (Validator.NormalizePhone(u.Phone) is string phone) u.Phone = phone;
                    break;
                case Categories c:
                    Add(Validator.ValidateRequired(c.CategoryName, "Категория", 100));
                    break;
                case Subcategories s:
                    Add(Validator.ValidateRequired(s.SubcategoryName, "Подкатегория", 100));
                    break;
                case Manufacturers m:
                    Add(Validator.ValidateRequired(m.ManufacturerName, "Производитель", 150));
                    Add(Validator.ValidateRequired(m.Country, "Страна", 100));
                    break;
                case Suppliers s:
                    Add(Validator.ValidateRequired(s.SupplierName, "Поставщик", 150));
                    Add(Validator.ValidatePhone(s.Phone));
                    Add(Validator.ValidateEmail(s.Email));
                    if (Validator.NormalizePhone(s.Phone) is string sphone) s.Phone = sphone;
                    break;
                case Products p:
                    Add(Validator.ValidateArticle(p.Article));
                    Add(Validator.ValidateRequired(p.Name, "Наименование", Validator.ProductNameMaxLength));
                    if (p.Price < 0) errors.Add("Цена не может быть отрицательной.");
                    if (p.DiscountPercent != null && (p.DiscountPercent < 1 || p.DiscountPercent > 99))
                        errors.Add("Скидка должна быть от 1 до 99 % или пустой.");
                    break;
                case Colors c:
                    Add(Validator.ValidateRequired(c.ColorName, "Цвет", 50));
                    break;
                case StockItems s:
                    if (s.Quantity < 0) errors.Add("Количество на складе не может быть отрицательным.");
                    break;
                case PickupPoints pp:
                    Add(Validator.ValidateRequired(pp.Address, "Адрес", 255));
                    Add(Validator.ValidateRequired(pp.WorkingHours, "Часы работы", 100));
                    break;
                case OrderStatuses st:
                    Add(Validator.ValidateRequired(st.StatusName, "Статус", 50));
                    break;
                case Orders o:
                    if (o.OrderNumber <= 0) errors.Add("Номер заказа должен быть больше нуля.");
                    if (o.PickupCode < 100 || o.PickupCode > 999) errors.Add("Код получения — три цифры (100–999).");
                    if (o.DeliveryDate != null && o.DeliveryDate.Value.Date < o.OrderDate.Date)
                        errors.Add("Дата доставки не может быть раньше даты заказа.");
                    break;
                case OrderItems oi:
                    if (oi.Quantity <= 0) errors.Add("Количество в заказе должно быть больше нуля.");
                    if (oi.PricePerUnit < 0) errors.Add("Цена не может быть отрицательной.");
                    break;
            }
            return errors;
        }

        /// <summary>
        /// Можно ли удалить запись (бизнес-ограничения, ТЗ F4).
        /// Возвращает текст запрета или null.
        /// </summary>
        public static string CheckDelete(object entity)
        {
            var db = AppConnect.model0db;
            switch (entity)
            {
                case Products p:
                    if (db.OrderItems.Any(oi => oi.StockItems.ProductId == p.Id))
                        return "Невозможно удалить товар, так как он присутствует в одном или нескольких заказах.";
                    break;
                case StockItems s:
                    if (db.OrderItems.Any(oi => oi.StockItemId == s.Id))
                        return "Невозможно удалить остаток, так как этот товар этого цвета есть в заказах.";
                    break;
                case Users u:
                    if (AppConnect.user != null && u.Id == AppConnect.user.Id)
                        return "Нельзя удалить пользователя, под которым вы вошли.";
                    if (db.Orders.Any(o => o.UserId == u.Id))
                        return "Невозможно удалить пользователя, так как у него есть заказы.";
                    break;
                case Roles r:
                    if (db.Users.Any(x => x.RoleId == r.Id))
                        return "Невозможно удалить роль, пока она назначена пользователям.";
                    break;
                case Categories c:
                    if (db.Subcategories.Any(x => x.CategoryId == c.Id))
                        return "Невозможно удалить категорию, в ней есть подкатегории.";
                    break;
                case Subcategories s:
                    if (db.Products.Any(x => x.SubcategoryId == s.Id))
                        return "Невозможно удалить подкатегорию, в ней есть товары.";
                    break;
                case Manufacturers m:
                    if (db.Products.Any(x => x.ManufacturerId == m.Id))
                        return "Невозможно удалить производителя, у него есть товары.";
                    break;
                case Suppliers s:
                    if (db.Products.Any(x => x.SupplierId == s.Id))
                        return "Невозможно удалить поставщика, у него есть товары.";
                    break;
                case Colors c:
                    if (db.StockItems.Any(x => x.ColorId == c.Id))
                        return "Невозможно удалить цвет, он указан в остатках товаров.";
                    break;
                case PickupPoints pp:
                    if (db.Orders.Any(x => x.PickupPointId == pp.Id))
                        return "Невозможно удалить пункт выдачи, он указан в заказах.";
                    break;
                case OrderStatuses st:
                    if (db.Orders.Any(x => x.StatusId == st.Id))
                        return "Невозможно удалить статус, он указан в заказах.";
                    break;
            }
            return null;
        }

        /// <summary>
        /// Удаляет запись вместе с зависимыми, которые можно удалить без потери данных:
        /// у товара — его остатки, у заказа — его позиции.
        /// </summary>
        public static void Remove(object entity)
        {
            var db = AppConnect.model0db;
            switch (entity)
            {
                case Products p:
                    db.StockItems.RemoveRange(db.StockItems.Where(s => s.ProductId == p.Id).ToList());
                    db.Products.Remove(p);
                    break;
                case Orders o:
                    db.OrderItems.RemoveRange(db.OrderItems.Where(i => i.OrderId == o.Id).ToList());
                    db.Orders.Remove(o);
                    break;
                default:
                    // GetObjectType: EF может вернуть прокси-класс вместо самой сущности
                    db.Set(ObjectContext.GetObjectType(entity.GetType())).Remove(entity);
                    break;
            }
        }
    }
}
