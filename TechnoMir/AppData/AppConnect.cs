using System.Data.Entity;
using System.Linq;

namespace TechnoMir.AppData
{
    /// <summary>Общие данные приложения: контекст БД и текущий пользователь.</summary>
    class AppConnect
    {
        public static TechnoMirEntities model0db;

        /// <summary>Вошедший пользователь (null — гость).</summary>
        public static Users user;

        /// <summary>Роль текущего пользователя (null — гость).</summary>
        public static int? RoleId => user?.RoleId;

        /// <summary>
        /// Отменяет несохранённые изменения в контексте, если SaveChanges упал:
        /// новые записи убираются, изменённые возвращаются к исходным значениям,
        /// помеченные на удаление остаются на месте.
        /// </summary>
        public static void RollbackChanges()
        {
            foreach (var entry in model0db.ChangeTracker.Entries().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }
    }
}
