using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Понятные сообщения об ошибках (ТЗ, F4). Используется в try/catch на страницах
    /// и в глобальном обработчике исключений в App.xaml.cs.
    /// </summary>
    public static class ErrorHandler
    {
        public static string GetFriendlyMessage(Exception ex)
        {
            if (ex is DbEntityValidationException validation)
            {
                var lines = validation.EntityValidationErrors
                    .SelectMany(v => v.ValidationErrors)
                    .Select(e => $"• {e.PropertyName}: {e.ErrorMessage}");
                return "Данные не прошли проверку:\n" + string.Join("\n", lines);
            }

            var sql = FindSqlException(ex);
            if (sql != null)
            {
                switch (sql.Number)
                {
                    case 547:
                        return "Операция нарушает связь между таблицами: запись используется в других " +
                               "таблицах или ссылается на несуществующую запись.";
                    case 2601:
                    case 2627:
                        return "Запись с таким значением уже существует. Это поле должно быть уникальным.";
                    case 515:
                        return "Не заполнено обязательное поле.";
                    case 8152:
                    case 2628:
                        return "Слишком длинное значение в одном из полей.";
                    case -1:
                    case 2:
                    case 53:
                    case 4060:
                    case 18456:
                        return "Нет подключения к базе данных. Проверьте, что SQL Server запущен, " +
                               "а строка подключения TechnoMirEntities в App.config указана верно.";
                }
                return "Ошибка базы данных: " + sql.Message;
            }

            if (ex is DbUpdateException)
                return "Не удалось сохранить изменения в базе данных.";
            if (ex is EntityException)
                return "Нет подключения к базе данных. Проверьте, что SQL Server запущен, " +
                       "а строка подключения TechnoMirEntities в App.config указана верно.";

            return "Произошла ошибка: " + ex.Message;
        }

        /// <summary>Показать ошибку пользователю во всплывающем окне.</summary>
        public static void Show(Exception ex, string title = "Ошибка")
        {
            MessageBox.Show(GetFriendlyMessage(ex), title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowWarning(string message, string title = "Проверьте данные")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void ShowInfo(string message, string title = "Информация")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool Confirm(string message, string title = "Подтверждение")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                   == MessageBoxResult.Yes;
        }

        private static SqlException FindSqlException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is SqlException sql) return sql;
                ex = ex.InnerException;
            }
            return null;
        }
    }
}
