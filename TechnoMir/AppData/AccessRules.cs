namespace TechnoMir.AppData
{
    /// <summary>
    /// Права ролей (ТЗ, F2). roleId = null — гость (не вошёл в систему).
    /// Коды ролей совпадают с таблицей Roles.
    /// </summary>
    public static class AccessRules
    {
        public const int AdminRoleId = 1;
        public const int ManagerRoleId = 2;
        public const int UserRoleId = 3;

        /// <summary>Роль, которая назначается при регистрации.</summary>
        public const int DefaultRoleId = UserRoleId;

        public static bool IsGuest(int? roleId) => roleId == null;
        public static bool IsAdmin(int? roleId) => roleId == AdminRoleId;
        public static bool IsManagerOrAdmin(int? roleId) => roleId == ManagerRoleId || roleId == AdminRoleId;

        /// <summary>Каталог, корзина, заказы, личный кабинет — всем, кто вошёл. Гостю только вход и регистрация.</summary>
        public static bool CanUseShop(int? roleId)
            => roleId == UserRoleId || IsManagerOrAdmin(roleId);

        /// <summary>Просмотр всех таблиц БД через меню.</summary>
        public static bool CanViewAllTables(int? roleId) => IsManagerOrAdmin(roleId);

        /// <summary>Добавление и изменение товаров и других записей (кроме пользователей).</summary>
        public static bool CanEditRecords(int? roleId) => IsManagerOrAdmin(roleId);

        /// <summary>Удаление записей — только администратор.</summary>
        public static bool CanDeleteRecords(int? roleId) => IsAdmin(roleId);

        /// <summary>Управление пользователями и их ролями — только администратор.</summary>
        public static bool CanManageUsers(int? roleId) => IsAdmin(roleId);

        /// <summary>Можно ли открыть таблицу в разделе «Таблицы БД».</summary>
        public static bool CanOpenTable(int? roleId, string tableName)
        {
            if (!CanViewAllTables(roleId)) return false;
            if (tableName == "Users" || tableName == "Roles") return CanManageUsers(roleId);
            return true;
        }
    }
}
