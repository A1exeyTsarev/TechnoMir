using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>История заказов текущего пользователя (ТЗ, F2).</summary>
    public partial class OrderHistoryPage : Page
    {
        public OrderHistoryPage()
        {
            InitializeComponent();
            if (AppConnect.user == null)
            {
                Loaded += (s, e) => AppFrame.mainFrame.Navigate(new AuthorizationPage());
                return;
            }

            int userId = AppConnect.user.Id;
            var orders = AppConnect.model0db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            lvOrders.ItemsSource = orders;
            txtEmpty.Visibility = orders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
    }
}
