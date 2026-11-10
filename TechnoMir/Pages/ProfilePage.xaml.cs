using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>Просмотр и изменение своих данных, кроме роли (ТЗ, F1).</summary>
    public partial class ProfilePage : Page
    {
        private readonly Users _user = AppConnect.user;

        public ProfilePage()
        {
            InitializeComponent();
            if (_user == null)
            {
                Loaded += (s, e) => AppFrame.mainFrame.Navigate(new AuthorizationPage());
                return;
            }
            FillFields();
        }

        private void FillFields()
        {
            txtSurname.Text = _user.Surname;
            txtFirstName.Text = _user.FirstName;
            txtPatronymic.Text = _user.Patronymic;
            txtEmail.Text = _user.Email;
            txtPhone.Text = _user.Phone;
            txtRole.Text = _user.RoleName;
            dpRegistrationDate.SelectedDate = _user.RegistrationDate;
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            void Check(string error) { if (error != null) errors.Add(error); }

            Check(Validator.ValidatePersonName(txtSurname.Text, "Фамилия"));
            Check(Validator.ValidatePersonName(txtFirstName.Text, "Имя"));
            Check(Validator.ValidatePersonName(txtPatronymic.Text, "Отчество", required: false));
            Check(Validator.ValidateEmail(txtEmail.Text));
            Check(Validator.ValidatePhone(txtPhone.Text));

            bool changePassword = pbCurrentPassword.Password.Length > 0
                                  || pbNewPassword.Password.Length > 0
                                  || pbNewPasswordConfirm.Password.Length > 0;
            if (changePassword)
            {
                if (!PasswordHasher.Verify(pbCurrentPassword.Password, _user.PasswordHash))
                    errors.Add("Текущий пароль введён неверно.");
                Check(Validator.ValidatePassword(pbNewPassword.Password));
                Check(Validator.ValidatePasswordConfirm(pbNewPassword.Password, pbNewPasswordConfirm.Password));
            }

            string email = txtEmail.Text.Trim();
            string phone = Validator.NormalizePhone(txtPhone.Text);
            if (errors.Count == 0)
            {
                if (AppConnect.model0db.Users.Any(u => u.Email == email && u.Id != _user.Id))
                    errors.Add("Этот email уже занят другим пользователем.");
                if (AppConnect.model0db.Users.Any(u => u.Phone == phone && u.Id != _user.Id))
                    errors.Add("Этот телефон уже занят другим пользователем.");
            }

            if (errors.Count > 0)
            {
                ErrorHandler.ShowWarning(string.Join("\n", errors));
                return;
            }

            _user.Surname = txtSurname.Text.Trim();
            _user.FirstName = txtFirstName.Text.Trim();
            _user.Patronymic = string.IsNullOrWhiteSpace(txtPatronymic.Text) ? null : txtPatronymic.Text.Trim();
            _user.Email = email;
            _user.Phone = phone;
            if (changePassword)
                _user.PasswordHash = PasswordHasher.GetHash(pbNewPassword.Password);

            try
            {
                AppConnect.model0db.SaveChanges();
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                FillFields();
                ErrorHandler.Show(ex, "Ошибка сохранения");
                return;
            }

            pbCurrentPassword.Clear();
            pbNewPassword.Clear();
            pbNewPasswordConfirm.Clear();
            txtPhone.Text = _user.Phone;
            MainWindow.Instance?.UpdateHeader();
            ErrorHandler.ShowInfo("Данные сохранены.", "Готово");
        }

        private void btnOrderHistory_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new OrderHistoryPage());

        private void btnBack_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new ProductCatalogPage());
    }
}
