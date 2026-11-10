using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechnoMir.AppData;

namespace TechnoMir.Tests
{
    /// <summary>Юнит-тесты расчёта цен (PriceCalculator).</summary>
    [TestClass]
    public class PriceCalculatorTests
    {
        [TestMethod] // UT-31
        public void CalculateFinalPrice_WithDiscount_ReturnsReducedPrice()
        {
            Assert.AreEqual(900m, PriceCalculator.CalculateFinalPrice(1000m, 10));
        }

        [TestMethod] // UT-32
        public void CalculateFinalPrice_NoDiscount_ReturnsSamePrice()
        {
            Assert.AreEqual(1500m, PriceCalculator.CalculateFinalPrice(1500m, null));
        }

        [TestMethod] // UT-33
        public void CalculateFinalPrice_RoundsToKopecks()
        {
            // 999.99 * 0.85 = 849.9915 → 849.99
            Assert.AreEqual(849.99m, PriceCalculator.CalculateFinalPrice(999.99m, 15));
        }

        [TestMethod] // UT-34
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CalculateFinalPrice_Discount100_Throws()
        {
            PriceCalculator.CalculateFinalPrice(1000m, 100);
        }

        [TestMethod] // UT-35
        public void IsExpensive_Exactly1000_ReturnsFalse()
        {
            Assert.IsFalse(PriceCalculator.IsExpensive(1000m));
        }

        [TestMethod] // UT-36
        public void IsExpensive_Above1000_ReturnsTrue()
        {
            Assert.IsTrue(PriceCalculator.IsExpensive(1000.01m));
        }
    }

    /// <summary>Юнит-тесты хеширования паролей (PasswordHasher).</summary>
    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod] // UT-37
        public void GetHash_KnownPassword_MatchesHashInDatabase()
        {
            // Такой же хеш лежит в Users.PasswordHash у администраторов
            Assert.AreEqual("3B612C75A7B5048A435FB6EC81E52FF92D6D795A8B5A9C17070F6A63C97A53B2",
                PasswordHasher.GetHash("Admin123"));
        }

        [TestMethod] // UT-38
        public void Verify_WrongPassword_ReturnsFalse()
        {
            string hash = PasswordHasher.GetHash("User123");
            Assert.IsFalse(PasswordHasher.Verify("user123", hash));
        }
    }

    /// <summary>Юнит-тесты прав ролей (AccessRules).</summary>
    [TestClass]
    public class AccessRulesTests
    {
        [TestMethod] // UT-39
        public void Guest_CannotUseShop()
        {
            Assert.IsFalse(AccessRules.CanUseShop(null));
        }

        [TestMethod] // UT-40
        public void Manager_CanEditButCannotDelete()
        {
            Assert.IsTrue(AccessRules.CanEditRecords(AccessRules.ManagerRoleId));
            Assert.IsFalse(AccessRules.CanDeleteRecords(AccessRules.ManagerRoleId));
        }

        [TestMethod] // UT-41
        public void Manager_CannotOpenUsersTable()
        {
            Assert.IsFalse(AccessRules.CanOpenTable(AccessRules.ManagerRoleId, "Users"));
        }

        [TestMethod] // UT-42
        public void Admin_CanManageUsersAndDelete()
        {
            Assert.IsTrue(AccessRules.CanManageUsers(AccessRules.AdminRoleId));
            Assert.IsTrue(AccessRules.CanDeleteRecords(AccessRules.AdminRoleId));
        }

        [TestMethod] // UT-43
        public void User_CannotViewAllTables()
        {
            Assert.IsFalse(AccessRules.CanViewAllTables(AccessRules.UserRoleId));
        }
    }

    /// <summary>Юнит-тесты корзины (ShoppingCart).</summary>
    [TestClass]
    public class ShoppingCartTests
    {
        private static CartItem Item(int stockId, int qty, decimal price)
            => new CartItem { StockItemId = stockId, ProductName = "Товар", Quantity = qty, PricePerUnit = price };

        [TestMethod] // UT-44
        public void Add_SameStockItemTwice_MergesQuantity()
        {
            var cart = new ShoppingCart();
            cart.Add(Item(1, 1, 500m));
            cart.Add(Item(1, 2, 500m));
            Assert.AreEqual(1, cart.Count);
            Assert.AreEqual(3, cart.GetQuantity(1));
        }

        [TestMethod] // UT-45
        public void TotalSum_SeveralItems_ReturnsSum()
        {
            var cart = new ShoppingCart();
            cart.Add(Item(1, 2, 500m));
            cart.Add(Item(2, 1, 1299.90m));
            Assert.AreEqual(2299.90m, cart.TotalSum);
        }

        [TestMethod] // UT-46
        public void Clear_RemovesAllItems()
        {
            var cart = new ShoppingCart();
            cart.Add(Item(1, 1, 100m));
            cart.Clear();
            Assert.AreEqual(0, cart.Count);
            Assert.AreEqual(0m, cart.TotalSum);
        }
    }
}
