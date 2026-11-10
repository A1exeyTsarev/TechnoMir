using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TechnoMir.AppData
{
    /// <summary>
    /// Проверка данных, которые вводит пользователь.
    /// Каждый метод Validate... возвращает текст ошибки или null, если всё верно.
    /// Класс не зависит от окон и базы данных, поэтому его проверяют юнит-тесты.
    /// </summary>
    public static class Validator
    {
        public const int NameMaxLength = 100;      // NVARCHAR(100) в Users
        public const int EmailMaxLength = 150;     // NVARCHAR(150) в Users
        public const int PasswordMinLength = 6;
        public const int PasswordMaxLength = 50;
        public const int SearchMaxLength = 100;
        public const int ArticleMaxLength = 20;    // NVARCHAR(20) в Products
        public const int ProductNameMaxLength = 255;
        public const int TextMaxLength = 500;      // описание и характеристики
        public const int MaxQuantity = 999;        // больше за один раз не заказывают
        public const decimal MaxPrice = 10000000m;

        private static readonly Regex EmailRegex =
            new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$");
        private static readonly Regex PersonNameRegex =
            new Regex(@"^[A-Za-zА-Яа-яЁё]+([ \-][A-Za-zА-Яа-яЁё]+)*$");
        private static readonly Regex ArticleRegex = new Regex(@"^[A-Za-z0-9\-]+$");

        // ---------------------------------------------------------- общие

        /// <summary>Обязательное текстовое поле с ограничением длины.</summary>
        public static string ValidateRequired(string value, string fieldName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return $"Заполните поле «{fieldName}».";
            if (value.Trim().Length > maxLength)
                return $"Поле «{fieldName}» не должно быть длиннее {maxLength} символов.";
            return null;
        }

        /// <summary>Необязательное текстовое поле: пустое можно, но не длиннее maxLength.</summary>
        public static string ValidateOptional(string value, string fieldName, int maxLength)
        {
            if (value != null && value.Trim().Length > maxLength)
                return $"Поле «{fieldName}» не должно быть длиннее {maxLength} символов.";
            return null;
        }

        /// <summary>Выбран ли элемент в выпадающем списке.</summary>
        public static string ValidateSelected(object selected, string what)
            => selected == null ? $"Выберите {what}." : null;

        /// <summary>Только цифры 0–9 (для PreviewTextInput полей количества).</summary>
        public static bool IsDigitsOnly(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
                if (c < '0' || c > '9') return false;
            return true;
        }

        // ---------------------------------------------------------- пользователь

        /// <summary>Фамилия, имя или отчество: буквы, пробел и дефис внутри, 2–100 символов.</summary>
        public static string ValidatePersonName(string value, string fieldName, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
                return required ? $"Заполните поле «{fieldName}»." : null;

            value = value.Trim();
            if (value.Length < 2)
                return $"Поле «{fieldName}» должно содержать не менее 2 букв.";
            if (value.Length > NameMaxLength)
                return $"Поле «{fieldName}» не должно быть длиннее {NameMaxLength} символов.";
            if (!PersonNameRegex.IsMatch(value))
                return $"Поле «{fieldName}» может содержать только буквы, пробел и дефис.";
            return null;
        }

        /// <summary>Email: формат имя@домен.зона, не длиннее 150 символов.</summary>
        public static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Введите email.";
            email = email.Trim();
            if (email.Length > EmailMaxLength)
                return $"Email не должен быть длиннее {EmailMaxLength} символов.";
            if (!EmailRegex.IsMatch(email))
                return "Email указан неверно. Пример: ivanov@mail.ru";
            return null;
        }

        /// <summary>
        /// Приводит телефон к виду +7XXXXXXXXXX. Принимает +7…, 7…, 8…,
        /// пробелы, скобки и дефисы. Если номер неверный — null.
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            string s = phone.Trim();
            bool plus = s.StartsWith("+");
            string digits = "";
            foreach (char c in s)
            {
                if (char.IsDigit(c)) digits += c;
                else if (c != '+' && c != ' ' && c != '(' && c != ')' && c != '-') return null;
            }

            if (digits.Length != 11) return null;
            if (plus && digits[0] != '7') return null;
            if (!plus && digits[0] != '7' && digits[0] != '8') return null;

            return "+7" + digits.Substring(1);
        }

        /// <summary>Телефон: российский номер из 11 цифр.</summary>
        public static string ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "Введите телефон.";
            if (NormalizePhone(phone) == null)
                return "Телефон указан неверно. Пример: +7 912 345-67-89";
            return null;
        }

        /// <summary>Пароль: 6–50 символов, без пробелов, хотя бы одна буква и одна цифра.</summary>
        public static string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Введите пароль.";
            if (password.Length < PasswordMinLength)
                return $"Пароль должен содержать не менее {PasswordMinLength} символов.";
            if (password.Length > PasswordMaxLength)
                return $"Пароль не должен быть длиннее {PasswordMaxLength} символов.";

            bool hasLetter = false, hasDigit = false;
            foreach (char c in password)
            {
                if (char.IsWhiteSpace(c)) return "Пароль не должен содержать пробелы.";
                if (char.IsLetter(c)) hasLetter = true;
                if (char.IsDigit(c)) hasDigit = true;
            }
            if (!hasLetter || !hasDigit)
                return "Пароль должен содержать хотя бы одну букву и одну цифру.";
            return null;
        }

        /// <summary>Повтор пароля совпадает с паролем.</summary>
        public static string ValidatePasswordConfirm(string password, string confirm)
            => password == confirm ? null : "Пароли не совпадают.";

        /// <summary>В строке входа email (есть @) или телефон.</summary>
        public static bool IsEmail(string login)
            => login != null && login.Contains("@");

        /// <summary>Строка входа: email или телефон.</summary>
        public static string ValidateLoginInput(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return "Введите email или телефон.";
            return IsEmail(login) ? ValidateEmail(login) : ValidatePhone(login);
        }

        // ---------------------------------------------------------- каталог и корзина

        /// <summary>Строка поиска: не длиннее SearchMaxLength.</summary>
        public static string ValidateSearch(string text)
        {
            if (text != null && text.Trim().Length > SearchMaxLength)
                return $"Строка поиска не должна быть длиннее {SearchMaxLength} символов.";
            return null;
        }

        /// <summary>Количество из текстового поля: целое 1..MaxQuantity и не больше остатка.</summary>
        public static string ValidateQuantity(string text, int available, out int quantity)
        {
            quantity = 0;

            if (string.IsNullOrWhiteSpace(text))
                return "Введите количество.";
            if (!int.TryParse(text.Trim(), out int q))
                return "Количество должно быть целым числом.";
            if (q <= 0)
                return "Количество должно быть больше нуля.";
            if (q > MaxQuantity)
                return $"За один раз можно заказать не более {MaxQuantity} шт.";
            if (available <= 0)
                return "Товара нет в наличии.";
            if (q > available)
                return $"Доступно только {available} шт.";

            quantity = q;
            return null;
        }

        /// <summary>Уже лежащее в корзине плюс новое количество не больше остатка.</summary>
        public static string ValidateAddToCart(int inCart, int adding, int available)
        {
            if (adding <= 0)
                return "Количество должно быть больше нуля.";
            if (inCart + adding > available)
                return $"Доступно {available} шт., в корзине уже {inCart} шт.";
            if (inCart + adding > MaxQuantity)
                return $"За один раз можно заказать не более {MaxQuantity} шт.";
            return null;
        }

        /// <summary>Корзина не пуста.</summary>
        public static string ValidateCartNotEmpty(int itemsCount)
            => itemsCount <= 0 ? "Корзина пуста." : null;

        // ---------------------------------------------------------- товар

        /// <summary>Артикул: латиница, цифры и дефис, до 20 символов.</summary>
        public static string ValidateArticle(string article)
        {
            string error = ValidateRequired(article, "Артикул", ArticleMaxLength);
            if (error != null) return error;
            if (!ArticleRegex.IsMatch(article.Trim()))
                return "Артикул может содержать только латинские буквы, цифры и дефис.";
            return null;
        }

        /// <summary>Цена: число от 0 до 10 000 000, не больше двух знаков после запятой.</summary>
        public static string ValidatePrice(string text, out decimal price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(text))
                return "Введите цену.";

            string s = text.Trim().Replace(" ", "").Replace(',', '.');
            if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p))
                return "Цена должна быть числом.";
            if (p < 0)
                return "Цена не может быть отрицательной.";
            if (p > MaxPrice)
                return $"Цена не может быть больше {MaxPrice:N0}.";
            if (decimal.Round(p, 2) != p)
                return "У цены не может быть больше двух знаков после запятой.";

            price = p;
            return null;
        }

        /// <summary>Скидка: пусто (нет скидки) или целое число от 1 до 99.</summary>
        public static string ValidateDiscount(string text, out int? discount)
        {
            discount = null;
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (!int.TryParse(text.Trim(), out int d))
                return "Скидка должна быть целым числом.";
            if (d < 1 || d > 99)
                return "Скидка должна быть от 1 до 99 %.";
            discount = d;
            return null;
        }

        /// <summary>Остаток на складе: целое число от 0.</summary>
        public static string ValidateStockQuantity(string text, out int quantity)
        {
            quantity = 0;
            if (string.IsNullOrWhiteSpace(text))
                return "Введите количество на складе.";
            if (!int.TryParse(text.Trim(), out int q))
                return "Количество на складе должно быть целым числом.";
            if (q < 0)
                return "Количество на складе не может быть отрицательным.";
            quantity = q;
            return null;
        }
    }
}
