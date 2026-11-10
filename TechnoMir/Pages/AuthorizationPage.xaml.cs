using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>
    /// Вход по email или телефону и паролю (ТЗ, F1).
    /// Гостю доступны только вход и регистрация (ТЗ, F2).
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        private const int MaxFailedAttempts = 3;
        private const int LockSeconds = 30;

        private static int _failedAttempts;
        private readonly DispatcherTimer _lockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private int _secondsLeft;

        public AuthorizationPage()
        {
            InitializeComponent();
            MainWindow.Instance?.UpdateHeader();
            _lockTimer.Tick += LockTimer_Tick;
            Loaded += (s, e) => txtLogin.Focus();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = pbPassword.Password;

            string error = Validator.ValidateLoginInput(login);
            if (error == null && string.IsNullOrEmpty(password))
                error = "Введите пароль.";
            if (error != null)
            {
                ErrorHandler.ShowWarning(error);
                return;
            }

            string hash = PasswordHasher.GetHash(password);
            Users user;
            if (Validator.IsEmail(login))
            {
                user = AppConnect.model0db.Users
                    .FirstOrDefault(u => u.Email == login && u.PasswordHash == hash);
            }
            else
            {
                string phone = Validator.NormalizePhone(login);
                user = AppConnect.model0db.Users
                    .FirstOrDefault(u => u.Phone == phone && u.PasswordHash == hash);
            }

            if (user == null)
            {
                _failedAttempts++;
                if (_failedAttempts >= MaxFailedAttempts)
                    StartLock();
                else
                    ErrorHandler.ShowWarning(
                        $"Неверный логин или пароль. Осталось попыток: {MaxFailedAttempts - _failedAttempts}.",
                        "Ошибка входа");
                pbPassword.Clear();
                return;
            }

            _failedAttempts = 0;
            AppConnect.user = user;
            ShoppingCart.Current.Clear();
            MainWindow.Instance?.UpdateHeader();
            AppFrame.mainFrame.Navigate(new ProductCatalogPage());
        }

        /// <summary>После трёх неудачных попыток вход блокируется на 30 секунд.</summary>
        private void StartLock()
        {
            _secondsLeft = LockSeconds;
            btnLogin.IsEnabled = false;
            txtLockInfo.Visibility = Visibility.Visible;
            txtLockInfo.Text = $"Слишком много неудачных попыток. Повторите через {_secondsLeft} с.";
            _lockTimer.Start();
        }

        private void LockTimer_Tick(object sender, EventArgs e)
        {
            _secondsLeft--;
            if (_secondsLeft > 0)
            {
                txtLockInfo.Text = $"Слишком много неудачных попыток. Повторите через {_secondsLeft} с.";
                return;
            }
            _lockTimer.Stop();
            _failedAttempts = 0;
            btnLogin.IsEnabled = true;
            txtLockInfo.Visibility = Visibility.Collapsed;
        }

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnLogin.IsEnabled)
                btnLogin_Click(sender, e);
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new RegistrationPage());
    }
}
