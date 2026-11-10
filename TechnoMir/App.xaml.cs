using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TechnoMir.AppData;

namespace TechnoMir
{
    /// <summary>
    /// Глобальная обработка исключений (ТЗ, F4): любая необработанная ошибка
    /// показывается пользователю понятным сообщением, программа не закрывается.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ошибки в потоке интерфейса (обработчики кнопок, загрузка страниц)
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Ошибки в других потоках
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Ошибки в задачах (Task), которые никто не дождался
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorHandler.Show(e.Exception, "Непредвиденная ошибка");
            e.Handled = true;   // программа продолжает работать
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                ErrorHandler.Show(ex, "Критическая ошибка");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.Invoke(() => ErrorHandler.Show(e.Exception.GetBaseException(), "Ошибка фоновой операции"));
            e.SetObserved();
        }
    }
}
