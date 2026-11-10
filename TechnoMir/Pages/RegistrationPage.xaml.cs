using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TechnoMir.AppData;

namespace TechnoMir.Pages
{
    /// <summary>Регистрация нового пользователя с ролью «Пользователь» (ТЗ, F1).</summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            void Check(string error) { if (error != null) errors.Add(error); }

            Check(Validator.ValidatePersonName(txtSurname.Text, "Фамилия"));
            Check(Validator.ValidatePersonName(txtFirstName.Text, "Имя"));
            Check(Validator.ValidatePersonName(txtPatronymic.Text, "Отчество", required: false));
            Check(Validator.ValidateEmail(txtEmail.Text));
            Check(Validator.ValidatePhone(txtPhone.Text));
            Check(Validator.ValidatePassword(pbPassword.Password));
            Check(Validator.ValidatePasswordConfirm(pbPassword.Password, pbPasswordConfirm.Password));

            string email = txtEmail.Text.Trim();
            string phone = Validator.NormalizePhone(txtPhone.Text);

            if (errors.Count == 0)
            {
                if (AppConnect.model0db.Users.Any(u => u.Email == email))
                    errors.Add("Пользователь с таким email уже зарегистрирован.");
                if (AppConnect.model0db.Users.Any(u => u.Phone == phone))
                    errors.Add("Пользователь с таким телефоном уже зарегистрирован.");
            }

            if (errors.Count > 0)
            {
                ErrorHandler.ShowWarning(string.Join("\n", errors));
                return;
            }

            var user = new Users
            {
                Surname = txtSurname.Text.Trim(),
                FirstName = txtFirstName.Text.Trim(),
                Patronymic = string.IsNullOrWhiteSpace(txtPatronymic.Text) ? null : txtPatronymic.Text.Trim(),
                Email = email,
                Phone = phone,
                PasswordHash = PasswordHasher.GetHash(pbPassword.Password),
                RegistrationDate = DateTime.Today,
                RoleId = AccessRules.DefaultRoleId
            };

            try
            {
                AppConnect.model0db.Users.Add(user);
                AppConnect.model0db.SaveChanges();
            }
            catch (Exception ex)
            {
                AppConnect.RollbackChanges();
                ErrorHandler.Show(ex, "Ошибка регистрации");
                return;
            }

            // Подгружаем роль, чтобы показать её в шапке
            AppConnect.model0db.Entry(user).Reference(u => u.Roles).Load();
            AppConnect.user = user;
            ShoppingCart.Current.Clear();
            MainWindow.Instance?.UpdateHeader();

            ErrorHandler.ShowInfo("Регистрация прошла успешно. Добро пожаловать!", "Готово");
            AppFrame.mainFrame.Navigate(new ProductCatalogPage());
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
            => AppFrame.mainFrame.Navigate(new AuthorizationPage());
    }
}
