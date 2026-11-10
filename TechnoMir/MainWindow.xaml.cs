using System.Windows;
using TechnoMir.AppData;

namespace TechnoMir
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppConnect.model0db = new TechnoMirEntities();
            AppFrame.mainFrame = MainFrame;
            MainFrame.Navigate(new Pages.AuthorizationPage());
        }

        /// <summary>Обновляет шапку после входа, выхода или изменения профиля.</summary>
        public void UpdateHeader()
        {
            var user = AppConnect.user;
            txtUserName.Text = user?.FIO ?? "";
            txtUserRole.Text = user?.RoleName ?? "";
            panelUserButtons.Visibility = user != null ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Удобный доступ к главному окну со страниц.</summary>
        public static MainWindow Instance => Application.Current.MainWindow as MainWindow;

        private void btnProfile_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new Pages.ProfilePage());

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!ErrorHandler.Confirm("Выйти из учётной записи?")) return;
            AppConnect.user = null;
            ShoppingCart.Current.Clear();
            UpdateHeader();
            AppFrame.mainFrame.Navigate(new Pages.AuthorizationPage());
        }
    }
}
