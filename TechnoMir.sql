-- ============================================================
--  База данных TechnoMir — ТехноМир — магазин электроники
--  MS SQL Server. 13 таблиц в 3НФ, первичные и внешние ключи,
--  ограничения UNIQUE/CHECK и тестовые данные.
--  Запуск: открыть в SSMS и нажать F5 (Выполнить).
--  ВНИМАНИЕ: если база TechnoMir уже есть, она будет удалена и создана заново.
--  Пароли тестовых пользователей (хранится SHA-256):
--    Администратор — Admin123, Менеджер — Manager123, Пользователь — User123
-- ============================================================
USE master;
GO

IF DB_ID(N'TechnoMir') IS NOT NULL
BEGIN
    ALTER DATABASE TechnoMir SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TechnoMir;
END
GO

CREATE DATABASE TechnoMir;
GO

USE TechnoMir;
GO

-- ============================================================
--  ТАБЛИЦЫ И СВЯЗИ
-- ============================================================

-- Роли
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) NOT NULL,
    RoleName NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

-- Пользователи
CREATE TABLE Users (
    Id INT IDENTITY(1,1) NOT NULL,
    Surname NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    Patronymic NVARCHAR(100) NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    PasswordHash NVARCHAR(64) NOT NULL,
    RegistrationDate DATE NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles (Id),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT UQ_Users_Phone UNIQUE (Phone),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%._%')
);
GO

-- Категории
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
);
GO

-- Подкатегории
CREATE TABLE Subcategories (
    Id INT IDENTITY(1,1) NOT NULL,
    SubcategoryName NVARCHAR(100) NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT PK_Subcategories PRIMARY KEY (Id),
    CONSTRAINT FK_Subcategories_Categories FOREIGN KEY (CategoryId) REFERENCES Categories (Id),
    CONSTRAINT UQ_Subcategories_SubcategoryName_CategoryId UNIQUE (SubcategoryName, CategoryId)
);
GO

-- Производители
CREATE TABLE Manufacturers (
    Id INT IDENTITY(1,1) NOT NULL,
    ManufacturerName NVARCHAR(150) NOT NULL,
    Country NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Manufacturers PRIMARY KEY (Id),
    CONSTRAINT UQ_Manufacturers_ManufacturerName UNIQUE (ManufacturerName)
);
GO

-- Поставщики
CREATE TABLE Suppliers (
    Id INT IDENTITY(1,1) NOT NULL,
    SupplierName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    CONSTRAINT PK_Suppliers PRIMARY KEY (Id),
    CONSTRAINT UQ_Suppliers_SupplierName UNIQUE (SupplierName)
);
GO

-- Товары
CREATE TABLE Products (
    Id INT IDENTITY(1,1) NOT NULL,
    Article NVARCHAR(20) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    SubcategoryId INT NOT NULL,
    ManufacturerId INT NOT NULL,
    SupplierId INT NOT NULL,
    Description NVARCHAR(500) NULL,
    Specifications NVARCHAR(500) NULL,
    ImagePath NVARCHAR(255) NULL,
    Price DECIMAL(10,2) NOT NULL,
    DiscountPercent INT NULL,
    CONSTRAINT PK_Products PRIMARY KEY (Id),
    CONSTRAINT FK_Products_Subcategories FOREIGN KEY (SubcategoryId) REFERENCES Subcategories (Id),
    CONSTRAINT FK_Products_Manufacturers FOREIGN KEY (ManufacturerId) REFERENCES Manufacturers (Id),
    CONSTRAINT FK_Products_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (Id),
    CONSTRAINT UQ_Products_Article UNIQUE (Article),
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_DiscountPercent CHECK (DiscountPercent IS NULL OR DiscountPercent BETWEEN 1 AND 99)
);
GO

-- Цвета
CREATE TABLE Colors (
    Id INT IDENTITY(1,1) NOT NULL,
    ColorName NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_Colors PRIMARY KEY (Id),
    CONSTRAINT UQ_Colors_ColorName UNIQUE (ColorName)
);
GO

-- Остатки на складе
CREATE TABLE StockItems (
    Id INT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    ColorId INT NOT NULL,
    Quantity INT NOT NULL,
    CONSTRAINT PK_StockItems PRIMARY KEY (Id),
    CONSTRAINT FK_StockItems_Products FOREIGN KEY (ProductId) REFERENCES Products (Id),
    CONSTRAINT FK_StockItems_Colors FOREIGN KEY (ColorId) REFERENCES Colors (Id),
    CONSTRAINT UQ_StockItems_ProductId_ColorId UNIQUE (ProductId, ColorId),
    CONSTRAINT CK_StockItems_Quantity CHECK (Quantity >= 0)
);
GO

-- Пункты выдачи
CREATE TABLE PickupPoints (
    Id INT IDENTITY(1,1) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    WorkingHours NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_PickupPoints PRIMARY KEY (Id),
    CONSTRAINT UQ_PickupPoints_Address UNIQUE (Address)
);
GO

-- Статусы заказа
CREATE TABLE OrderStatuses (
    Id INT IDENTITY(1,1) NOT NULL,
    StatusName NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_OrderStatuses PRIMARY KEY (Id),
    CONSTRAINT UQ_OrderStatuses_StatusName UNIQUE (StatusName)
);
GO

-- Заказы
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) NOT NULL,
    OrderNumber INT NOT NULL,
    OrderDate DATETIME NOT NULL,
    DeliveryDate DATE NULL,
    UserId INT NOT NULL,
    PickupPointId INT NOT NULL,
    StatusId INT NOT NULL,
    PickupCode INT NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (Id),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users (Id),
    CONSTRAINT FK_Orders_PickupPoints FOREIGN KEY (PickupPointId) REFERENCES PickupPoints (Id),
    CONSTRAINT FK_Orders_OrderStatuses FOREIGN KEY (StatusId) REFERENCES OrderStatuses (Id),
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber),
    CONSTRAINT CK_Orders_PickupCode CHECK (PickupCode BETWEEN 100 AND 999),
    CONSTRAINT CK_Orders_DeliveryDate CHECK (DeliveryDate IS NULL OR DeliveryDate >= CAST(OrderDate AS DATE))
);
GO

-- Состав заказа
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) NOT NULL,
    OrderId INT NOT NULL,
    StockItemId INT NOT NULL,
    Quantity INT NOT NULL,
    PricePerUnit DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (Id),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_StockItems FOREIGN KEY (StockItemId) REFERENCES StockItems (Id),
    CONSTRAINT UQ_OrderItems_OrderId_StockItemId UNIQUE (OrderId, StockItemId),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_PricePerUnit CHECK (PricePerUnit >= 0)
);
GO

-- ============================================================
--  ДАННЫЕ
-- ============================================================

-- Роли
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (Id, RoleName) VALUES
    (1, N'Администратор'),
    (2, N'Менеджер'),
    (3, N'Пользователь');
SET IDENTITY_INSERT Roles OFF;
GO

-- Пользователи
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, Surname, FirstName, Patronymic, Email, Phone, PasswordHash, RegistrationDate, RoleId) VALUES
    (1, N'Смирнов', N'Алексей', N'Викторович', N'asmirnov@technomir.ru', N'+79158813998', N'3B612C75A7B5048A435FB6EC81E52FF92D6D795A8B5A9C17070F6A63C97A53B2', '2026-04-27', 1),
    (2, N'Кузнецова', N'Мария', N'Игоревна', N'mkuznecova@technomir.ru', N'+79987973091', N'3B612C75A7B5048A435FB6EC81E52FF92D6D795A8B5A9C17070F6A63C97A53B2', '2026-02-07', 1),
    (3, N'Попов', N'Дмитрий', N'Андреевич', N'dpopov@technomir.ru', N'+79417075364', N'61B3AD24336C20515573DC7D875CB9295E8462CEC645C3604AC58F1E241E20E0', '2026-04-09', 2),
    (4, N'Васильева', N'Екатерина', N'Олеговна', N'evasileva@technomir.ru', N'+79568151884', N'61B3AD24336C20515573DC7D875CB9295E8462CEC645C3604AC58F1E241E20E0', '2026-03-24', 2),
    (5, N'Соколов', N'Артём', N'Павлович', N'asokolov@technomir.ru', N'+79729405576', N'61B3AD24336C20515573DC7D875CB9295E8462CEC645C3604AC58F1E241E20E0', '2026-02-02', 2),
    (6, N'Михайлова', N'Ольга', N'Сергеевна', N'omihaylova@mail.ru', N'+79698716897', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-04-21', 3),
    (7, N'Новиков', N'Игорь', N'Николаевич', N'inovikov@mail.ru', N'+79846978809', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-03-08', 3),
    (8, N'Фёдорова', N'Анастасия', N'Романовна', N'afedorova@mail.ru', N'+79208196727', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-03-09', 3),
    (9, N'Морозов', N'Кирилл', N'Максимович', N'kmorozov@mail.ru', N'+79177848648', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-06-24', 3),
    (10, N'Волкова', N'Дарья', N'Александровна', N'dvolkova@mail.ru', N'+79876634400', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-02-17', 3),
    (11, N'Алексеев', N'Максим', N'Евгеньевич', N'malekseev@mail.ru', N'+79978753477', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-05-20', 3),
    (12, N'Лебедева', N'Полина', N'Юрьевна', N'plebedeva@mail.ru', N'+79768045717', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-07-05', 3),
    (13, N'Семёнов', N'Никита', N'Владимирович', N'nsemenov@mail.ru', N'+79847267147', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-04-14', 3),
    (14, N'Егорова', N'Виктория', N'Денисовна', N'vegorova@mail.ru', N'+79136270107', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-06-07', 3),
    (15, N'Павлов', N'Роман', N'Ильич', N'rpavlov@mail.ru', N'+79819331908', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-03-19', 3),
    (16, N'Козлова', N'Алина', N'Тимуровна', N'akozlova@mail.ru', N'+79520599728', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-03-08', 3),
    (17, N'Степанов', N'Егор', N'Валерьевич', N'estepanov@mail.ru', N'+79792984162', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-02-13', 3),
    (18, N'Николаева', N'Софья', N'Артёмовна', N'snikolaeva@mail.ru', N'+79948662836', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-01-13', 3),
    (19, N'Орлов', N'Тимофей', N'Глебович', N'torlov@mail.ru', N'+79161055008', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-05-11', 3),
    (20, N'Андреева', N'Ксения', N'Михайловна', N'kandreeva@mail.ru', N'+79547517799', N'A61A8ADF60038792A2CB88E670B20540A9D6C2CA204AB754FC768950E79E7D36', '2026-02-22', 3);
SET IDENTITY_INSERT Users OFF;
GO

-- Категории
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (Id, CategoryName) VALUES
    (1, N'Смартфоны и гаджеты'),
    (2, N'Компьютерная техника'),
    (3, N'ТВ, аудио и игры');
SET IDENTITY_INSERT Categories OFF;
GO

-- Подкатегории
SET IDENTITY_INSERT Subcategories ON;
INSERT INTO Subcategories (Id, SubcategoryName, CategoryId) VALUES
    (1, N'Смартфоны', 1),
    (2, N'Планшеты', 1),
    (3, N'Умные часы', 1),
    (4, N'Наушники', 1),
    (5, N'Ноутбуки', 2),
    (6, N'Мониторы', 2),
    (7, N'Клавиатуры и мыши', 2),
    (8, N'Принтеры', 2),
    (9, N'Телевизоры', 3),
    (10, N'Портативные колонки', 3),
    (11, N'Игровые консоли', 3);
SET IDENTITY_INSERT Subcategories OFF;
GO

-- Производители
SET IDENTITY_INSERT Manufacturers ON;
INSERT INTO Manufacturers (Id, ManufacturerName, Country) VALUES
    (1, N'Apple', N'США'),
    (2, N'Samsung', N'Южная Корея'),
    (3, N'Xiaomi', N'Китай'),
    (4, N'Huawei', N'Китай'),
    (5, N'Honor', N'Китай'),
    (6, N'Lenovo', N'Китай'),
    (7, N'ASUS', N'Тайвань'),
    (8, N'Acer', N'Тайвань'),
    (9, N'HP', N'США'),
    (10, N'MSI', N'Тайвань'),
    (11, N'Logitech', N'Швейцария'),
    (12, N'Sony', N'Япония'),
    (13, N'LG', N'Южная Корея'),
    (14, N'JBL', N'США'),
    (15, N'Canon', N'Япония'),
    (16, N'Philips', N'Нидерланды');
SET IDENTITY_INSERT Manufacturers OFF;
GO

-- Поставщики
SET IDENTITY_INSERT Suppliers ON;
INSERT INTO Suppliers (Id, SupplierName, Phone, Email) VALUES
    (1, N'ООО «ЦифроОпт»', N'+74951234501', N'opt@cifroopt.ru'),
    (2, N'ООО «ТехДистрибуция»', N'+74951234502', N'sales@techdistr.ru'),
    (3, N'АО «Электрон-Снаб»', N'+78121234503', N'info@elsnab.ru'),
    (4, N'ИП Корнилов Д. А.', N'+79031234504', N'kornilov.da@mail.ru');
SET IDENTITY_INSERT Suppliers OFF;
GO

-- Товары
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (Id, Article, Name, SubcategoryId, ManufacturerId, SupplierId, Description, Specifications, ImagePath, Price, DiscountPercent) VALUES
    (1, N'TM-0001', N'Смартфон Galaxy A55 8/256 ГБ', 1, 2, 1, N'Смартфон среднего класса с ярким экраном и защитой от воды IP67.', N'Экран 6.6" Super AMOLED 120 Гц; память 8/256 ГБ; камера 50 Мп; аккумулятор 5000 мА·ч', N'ProductImages/phone_galaxy_a55.png', 38990, NULL),
    (2, N'TM-0002', N'Смартфон iPhone 15 128 ГБ', 1, 1, 2, N'Смартфон с динамическим островом и разъёмом USB-C.', N'Экран 6.1" OLED; память 128 ГБ; камера 48 Мп; процессор A16 Bionic', N'ProductImages/phone_iphone15.png', 79990, 10),
    (3, N'TM-0003', N'Смартфон Redmi Note 13 Pro 8/256 ГБ', 1, 3, 3, N'Смартфон с камерой 200 Мп и быстрой зарядкой 67 Вт.', N'Экран 6.67" AMOLED 120 Гц; память 8/256 ГБ; камера 200 Мп; аккумулятор 5100 мА·ч', N'ProductImages/phone_redmi13pro.png', 27990, NULL),
    (4, N'TM-0004', N'Смартфон Honor 200 12/256 ГБ', 1, 5, 4, N'Смартфон с портретной камерой и тонким корпусом.', N'Экран 6.7" OLED 120 Гц; память 12/256 ГБ; камера 50 Мп; аккумулятор 5200 мА·ч', N'ProductImages/phone_honor200.png', 34990, NULL),
    (5, N'TM-0005', N'Планшет iPad 10.9 64 ГБ Wi-Fi', 2, 1, 1, N'Планшет для учёбы, работы и развлечений.', N'Экран 10.9" Liquid Retina; память 64 ГБ; процессор A14 Bionic; Wi-Fi 6', N'ProductImages/tablet_ipad.png', 44990, 15),
    (6, N'TM-0006', N'Планшет Tab P12 8/128 ГБ', 2, 6, 2, N'Планшет с большим экраном 3K и четырьмя динамиками.', N'Экран 12.7" 3K; память 8/128 ГБ; аккумулятор 10200 мА·ч', NULL, 29990, NULL),
    (7, N'TM-0007', N'Умные часы Galaxy Watch6 44 мм', 3, 2, 3, N'Умные часы с измерением пульса, сна и уровня кислорода.', N'Экран 1.5" Super AMOLED; защита 5ATM; GPS; NFC', N'ProductImages/watch_galaxy6.png', 24990, NULL),
    (8, N'TM-0008', N'Умные часы Watch GT 4 46 мм', 3, 4, 4, N'Часы с автономностью до 14 дней.', N'Экран 1.43" AMOLED; защита 5ATM; GPS; более 100 режимов тренировок', N'ProductImages/watch_gt4.png', 19990, NULL),
    (9, N'TM-0009', N'Наушники WH-1000XM5', 4, 12, 1, N'Полноразмерные беспроводные наушники с шумоподавлением.', N'Bluetooth 5.2; активное шумоподавление; до 30 ч работы', N'ProductImages/headphones_xm5.png', 32990, 20),
    (10, N'TM-0010', N'Наушники AirPods Pro 2', 4, 1, 2, N'Беспроводные наушники-вкладыши с шумоподавлением.', N'Bluetooth 5.3; активное шумоподавление; кейс USB-C; до 6 ч работы', N'ProductImages/earbuds_airpods.png', 24990, NULL),
    (11, N'TM-0011', N'Ноутбук VivoBook 15 X1504', 5, 7, 3, N'Ноутбук для учёбы и офисной работы.', N'Экран 15.6" Full HD; Intel Core i5-1235U; ОЗУ 16 ГБ; SSD 512 ГБ', N'ProductImages/laptop_vivobook.png', 54990, NULL),
    (12, N'TM-0012', N'Ноутбук Aspire 5 A515', 5, 8, 4, N'Универсальный ноутбук с металлической крышкой.', N'Экран 15.6" IPS; Intel Core i5-1335U; ОЗУ 8 ГБ; SSD 512 ГБ', N'ProductImages/laptop_aspire5.png', 49990, NULL),
    (13, N'TM-0013', N'Ноутбук IdeaPad Slim 3', 5, 6, 1, N'Тонкий и лёгкий ноутбук.', N'Экран 15.6" Full HD; AMD Ryzen 5 7520U; ОЗУ 8 ГБ; SSD 256 ГБ', NULL, 47990, 5),
    (14, N'TM-0014', N'Монитор 27" UltraGear 27GR75Q', 6, 13, 2, N'Игровой монитор с частотой 165 Гц.', N'Диагональ 27"; 2560×1440; IPS; 165 Гц; 1 мс', N'ProductImages/monitor_lg27.png', 26990, NULL),
    (15, N'TM-0015', N'Монитор 24" G244F', 6, 10, 3, N'Игровой монитор с частотой 170 Гц.', N'Диагональ 23.8"; 1920×1080; IPS; 170 Гц; 1 мс', NULL, 15990, NULL),
    (16, N'TM-0016', N'Мышь беспроводная MX Master 3S', 7, 11, 4, N'Эргономичная мышь с тихими кнопками.', N'8000 dpi; Bluetooth и приёмник; аккумулятор до 70 дней', N'ProductImages/mouse_mx3s.png', 9990, 25),
    (17, N'TM-0017', N'Игровая консоль PlayStation 5 Slim', 11, 12, 1, N'Игровая консоль нового поколения с дисководом.', N'SSD 1 ТБ; 4K 120 Гц; HDR; геймпад DualSense в комплекте', N'ProductImages/console_ps5.png', 64990, NULL),
    (18, N'TM-0018', N'Принтер лазерный LaserJet M111w', 8, 9, 2, N'Компактный чёрно-белый лазерный принтер с Wi-Fi.', N'Печать до 20 стр/мин; формат A4; Wi-Fi; USB', NULL, 12990, 30),
    (19, N'TM-0019', N'Телевизор 55" QLED Q60D', 9, 2, 3, N'Телевизор 4K со Smart TV.', N'Диагональ 55"; 3840×2160; QLED; Smart TV Tizen; HDR10+', N'ProductImages/tv_q60d.png', 59990, NULL),
    (20, N'TM-0020', N'Колонка портативная Charge 5', 10, 14, 4, N'Портативная колонка с защитой от воды и пыли.', N'Мощность 40 Вт; Bluetooth 5.1; IP67; до 20 ч работы', N'ProductImages/speaker_charge5.png', 15990, NULL),
    (21, N'TM-0021', N'Наушники проводные EarPods USB-C', 4, 1, 1, N'Проводные наушники с пультом и микрофоном.', N'Разъём USB-C; длина кабеля 1,1 м', NULL, 2490, NULL),
    (22, N'TM-0022', N'Кабель USB-C – USB-C 1 м', 7, 3, 2, N'Кабель для зарядки и передачи данных.', N'Длина 1 м; до 60 Вт; USB 2.0', NULL, 590, NULL),
    (23, N'TM-0023', N'Чехол-книжка для Galaxy A55', 1, 2, 3, N'Чехол с подставкой и отделением для карт.', N'Экокожа; магнитная застёжка', NULL, 990, NULL);
SET IDENTITY_INSERT Products OFF;
GO

-- Цвета
SET IDENTITY_INSERT Colors ON;
INSERT INTO Colors (Id, ColorName) VALUES
    (1, N'Чёрный'),
    (2, N'Белый'),
    (3, N'Серебристый'),
    (4, N'Серый'),
    (5, N'Синий'),
    (6, N'Золотой'),
    (7, N'Зелёный'),
    (8, N'Фиолетовый'),
    (9, N'Красный'),
    (10, N'Розовый');
SET IDENTITY_INSERT Colors OFF;
GO

-- Остатки на складе
SET IDENTITY_INSERT StockItems ON;
INSERT INTO StockItems (Id, ProductId, ColorId, Quantity) VALUES
    (1, 1, 1, 6),
    (2, 1, 5, 4),
    (3, 1, 8, 2),
    (4, 2, 1, 3),
    (5, 2, 2, 2),
    (6, 2, 10, 1),
    (7, 3, 1, 8),
    (8, 3, 8, 5),
    (9, 4, 1, 2),
    (10, 4, 2, 1),
    (11, 5, 4, 4),
    (12, 5, 3, 3),
    (13, 5, 5, 2),
    (14, 6, 4, 5),
    (15, 7, 1, 3),
    (16, 7, 3, 4),
    (17, 8, 1, 2),
    (18, 8, 4, 0),
    (19, 9, 1, 4),
    (20, 9, 3, 3),
    (21, 10, 2, 10),
    (22, 11, 3, 5),
    (23, 11, 5, 2),
    (24, 12, 4, 4),
    (25, 13, 4, 3),
    (26, 13, 5, 3),
    (27, 14, 1, 6),
    (28, 15, 1, 1),
    (29, 16, 4, 7),
    (30, 16, 2, 5),
    (31, 17, 2, 3),
    (32, 17, 1, 0),
    (33, 18, 2, 4),
    (34, 19, 1, 5),
    (35, 20, 1, 6),
    (36, 20, 5, 4),
    (37, 20, 9, 3),
    (38, 20, 7, 2),
    (39, 21, 2, 15),
    (40, 22, 1, 30),
    (41, 22, 2, 25),
    (42, 23, 1, 8),
    (43, 23, 5, 0);
SET IDENTITY_INSERT StockItems OFF;
GO

-- Пункты выдачи
SET IDENTITY_INSERT PickupPoints ON;
INSERT INTO PickupPoints (Id, Address, WorkingHours) VALUES
    (1, N'г. Москва, ул. Тверская, д. 12', N'Пн–Вс 10:00–21:00'),
    (2, N'г. Москва, Ленинский пр-т, д. 45', N'Пн–Вс 10:00–22:00'),
    (3, N'г. Москва, ул. Профсоюзная, д. 78', N'Пн–Сб 09:00–20:00'),
    (4, N'г. Москва, Варшавское ш., д. 21', N'Пн–Вс 10:00–21:00'),
    (5, N'г. Москва, ул. Новый Арбат, д. 8', N'Пн–Вс 11:00–22:00'),
    (6, N'г. Москва, Дмитровское ш., д. 89', N'Пн–Пт 09:00–19:00');
SET IDENTITY_INSERT PickupPoints OFF;
GO

-- Статусы заказа
SET IDENTITY_INSERT OrderStatuses ON;
INSERT INTO OrderStatuses (Id, StatusName) VALUES
    (1, N'Новый'),
    (2, N'В обработке'),
    (3, N'Собран'),
    (4, N'Выдан'),
    (5, N'Отменён');
SET IDENTITY_INSERT OrderStatuses OFF;
GO

-- Заказы
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (Id, OrderNumber, OrderDate, DeliveryDate, UserId, PickupPointId, StatusId, PickupCode) VALUES
    (1, 1, '2026-08-12T10:15:00', '2026-08-15', 6, 5, 4, 632),
    (2, 2, '2026-08-25T14:40:00', '2026-08-28', 9, 4, 4, 738),
    (3, 3, '2026-09-03T11:05:00', '2026-09-07', 12, 4, 4, 712),
    (4, 4, '2026-09-20T16:30:00', '2026-09-23', 7, 6, 4, 827),
    (5, 5, '2026-10-05T12:00:00', '2026-10-07', 15, 5, 4, 508),
    (6, 6, '2026-10-18T18:20:00', '2026-10-20', 6, 2, 4, 762),
    (7, 7, '2026-10-27T09:45:00', '2026-10-30', 18, 1, 3, 763),
    (8, 8, '2026-11-01T13:10:00', '2026-11-03', 10, 4, 1, 988),
    (9, 9, '2026-11-10T18:25:00', '2026-11-15', 17, 6, 5, 998),
    (10, 10, '2026-11-19T22:40:00', '2026-11-22', 16, 2, 1, 499),
    (11, 11, '2026-11-26T00:55:00', '2026-11-30', 8, 3, 5, 575),
    (12, 12, '2026-12-01T05:40:00', '2026-12-06', 7, 5, 1, 915);
SET IDENTITY_INSERT Orders OFF;
GO

-- Состав заказа
SET IDENTITY_INSERT OrderItems ON;
INSERT INTO OrderItems (Id, OrderId, StockItemId, Quantity, PricePerUnit) VALUES
    (1, 1, 1, 1, 38990),
    (2, 1, 19, 1, 32990),
    (3, 2, 22, 1, 54990),
    (4, 3, 7, 2, 27990),
    (5, 4, 21, 1, 24990),
    (6, 4, 29, 1, 9990),
    (7, 5, 11, 1, 44990),
    (8, 6, 36, 1, 15990),
    (9, 6, 15, 1, 24990),
    (10, 7, 4, 1, 79990),
    (11, 8, 34, 1, 59990),
    (12, 8, 27, 1, 26990),
    (13, 9, 9, 1, 34990),
    (14, 10, 29, 2, 7492.5),
    (15, 10, 37, 1, 15990),
    (16, 11, 28, 2, 15990),
    (17, 11, 20, 1, 26392.0),
    (18, 12, 19, 1, 26392.0),
    (19, 12, 7, 1, 27990),
    (20, 12, 14, 1, 29990);
SET IDENTITY_INSERT OrderItems OFF;
GO
