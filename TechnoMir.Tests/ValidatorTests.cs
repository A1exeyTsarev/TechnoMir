using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechnoMir.AppData;

namespace TechnoMir.Tests
{
    /// <summary>
    /// Юнит-тесты проверки ввода (класс Validator).
    /// Номера UT-.. совпадают с таблицей в документе «Юнит-тесты ТехноМир».
    /// </summary>
    [TestClass]
    public class ValidatorTests
    {
        // ---------- Email ----------

        [TestMethod] // UT-01
        public void ValidateEmail_Correct_ReturnsNull()
        {
            Assert.IsNull(Validator.ValidateEmail("omihaylova@mail.ru"));
        }

        [TestMethod] // UT-02
        public void ValidateEmail_Empty_ReturnsError()
        {
            Assert.AreEqual("Введите email.", Validator.ValidateEmail("   "));
        }

        [TestMethod] // UT-03
        public void ValidateEmail_WithoutAt_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidateEmail("omihaylova.mail.ru"));
        }

        [TestMethod] // UT-04
        public void ValidateEmail_WithoutDomainZone_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidateEmail("user@mail"));
        }

        // ---------- Телефон ----------

        [TestMethod] // UT-05
        public void NormalizePhone_WithEightAndSeparators_ReturnsPlusSeven()
        {
            Assert.AreEqual("+79123456789", Validator.NormalizePhone("8 (912) 345-67-89"));
        }

        [TestMethod] // UT-06
        public void NormalizePhone_PlusSeven_ReturnsSame()
        {
            Assert.AreEqual("+79123456789", Validator.NormalizePhone("+79123456789"));
        }

        [TestMethod] // UT-07
        public void ValidatePhone_TooShort_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidatePhone("+7912345"));
        }

        [TestMethod] // UT-08
        public void ValidatePhone_WithLetters_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidatePhone("+7912abc6789"));
        }

        // ---------- Пароль ----------

        [TestMethod] // UT-09
        public void ValidatePassword_Correct_ReturnsNull()
        {
            Assert.IsNull(Validator.ValidatePassword("User123"));
        }

        [TestMethod] // UT-10
        public void ValidatePassword_TooShort_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidatePassword("ab1"));
        }

        [TestMethod] // UT-11
        public void ValidatePassword_OnlyLetters_ReturnsError()
        {
            Assert.AreEqual("Пароль должен содержать хотя бы одну букву и одну цифру.",
                Validator.ValidatePassword("password"));
        }

        [TestMethod] // UT-12
        public void ValidatePassword_WithSpace_ReturnsError()
        {
            Assert.AreEqual("Пароль не должен содержать пробелы.", Validator.ValidatePassword("User 123"));
        }

        [TestMethod] // UT-13
        public void ValidatePasswordConfirm_Different_ReturnsError()
        {
            Assert.AreEqual("Пароли не совпадают.", Validator.ValidatePasswordConfirm("User123", "User124"));
        }

        // ---------- ФИО ----------

        [TestMethod] // UT-14
        public void ValidatePersonName_DoubleSurname_ReturnsNull()
        {
            Assert.IsNull(Validator.ValidatePersonName("Римская-Корсакова", "Фамилия"));
        }

        [TestMethod] // UT-15
        public void ValidatePersonName_WithDigits_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidatePersonName("Иван1", "Имя"));
        }

        [TestMethod] // UT-16
        public void ValidatePersonName_EmptyOptional_ReturnsNull()
        {
            Assert.IsNull(Validator.ValidatePersonName("", "Отчество", required: false));
        }

        // ---------- Вход ----------

        [TestMethod] // UT-17
        public void ValidateLoginInput_Phone_ReturnsNull()
        {
            Assert.IsNull(Validator.ValidateLoginInput("+7 912 345-67-89"));
        }

        [TestMethod] // UT-18
        public void ValidateLoginInput_Empty_ReturnsError()
        {
            Assert.AreEqual("Введите email или телефон.", Validator.ValidateLoginInput(""));
        }

        // ---------- Количество и корзина ----------

        [TestMethod] // UT-19
        public void ValidateQuantity_Correct_ReturnsNullAndValue()
        {
            Assert.IsNull(Validator.ValidateQuantity("2", 5, out int q));
            Assert.AreEqual(2, q);
        }

        [TestMethod] // UT-20
        public void ValidateQuantity_MoreThanStock_ReturnsError()
        {
            Assert.AreEqual("Доступно только 3 шт.", Validator.ValidateQuantity("4", 3, out _));
        }

        [TestMethod] // UT-21
        public void ValidateQuantity_Zero_ReturnsError()
        {
            Assert.AreEqual("Количество должно быть больше нуля.", Validator.ValidateQuantity("0", 3, out _));
        }

        [TestMethod] // UT-22
        public void ValidateAddToCart_TotalMoreThanStock_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidateAddToCart(inCart: 2, adding: 2, available: 3));
        }

        [TestMethod] // UT-23
        public void ValidateCartNotEmpty_Empty_ReturnsError()
        {
            Assert.AreEqual("Корзина пуста.", Validator.ValidateCartNotEmpty(0));
        }

        // ---------- Товар ----------

        [TestMethod] // UT-24
        public void ValidatePrice_CommaDecimal_ReturnsNullAndValue()
        {
            Assert.IsNull(Validator.ValidatePrice("1299,90", out decimal price));
            Assert.AreEqual(1299.90m, price);
        }

        [TestMethod] // UT-25
        public void ValidatePrice_Negative_ReturnsError()
        {
            Assert.AreEqual("Цена не может быть отрицательной.", Validator.ValidatePrice("-5", out _));
        }

        [TestMethod] // UT-26
        public void ValidatePrice_ThreeDecimals_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidatePrice("10.555", out _));
        }

        [TestMethod] // UT-27
        public void ValidateDiscount_Empty_ReturnsNullWithoutDiscount()
        {
            Assert.IsNull(Validator.ValidateDiscount("", out int? discount));
            Assert.IsNull(discount);
        }

        [TestMethod] // UT-28
        public void ValidateDiscount_OutOfRange_ReturnsError()
        {
            Assert.AreEqual("Скидка должна быть от 1 до 99 %.", Validator.ValidateDiscount("100", out _));
        }

        [TestMethod] // UT-29
        public void ValidateArticle_Cyrillic_ReturnsError()
        {
            Assert.IsNotNull(Validator.ValidateArticle("ТМ-0001"));
        }

        [TestMethod] // UT-30
        public void IsDigitsOnly_Letter_ReturnsFalse()
        {
            Assert.IsFalse(Validator.IsDigitsOnly("1a"));
        }
    }
}
