DROP DATABASE IF EXISTS Trade;
CREATE DATABASE Trade;
USE Trade;

-- таблица Ролей
CREATE TABLE Role (
    RoleID INT PRIMARY KEY AUTO_INCREMENT,
    RoleName VARCHAR(100) NOT NULL
);

-- таблица Пользователей
CREATE TABLE User (
    UserID INT PRIMARY KEY AUTO_INCREMENT,
    UserSurname VARCHAR(100) NOT NULL,
    UserName VARCHAR(100) NOT NULL,
    UserPatronymic VARCHAR(100) NOT NULL,
    UserLogin VARCHAR(100) NOT NULL UNIQUE,
    UserPassword VARCHAR(100) NOT NULL,
    UserRole INT NOT NULL,
    FOREIGN KEY (UserRole) REFERENCES Role(RoleID)
);

-- справочники для товаров
CREATE TABLE ProductCategory (
    CategoryID INT PRIMARY KEY AUTO_INCREMENT,
    CategoryName VARCHAR(100) NOT NULL
);

CREATE TABLE ProductManufacturer (
    ManufacturerID INT PRIMARY KEY AUTO_INCREMENT,
    ManufacturerName VARCHAR(100) NOT NULL
);

CREATE TABLE ProductSupplier (
    SupplierID INT PRIMARY KEY AUTO_INCREMENT,
    SupplierName VARCHAR(100) NOT NULL
);

CREATE TABLE UnitType (
    UnitID INT PRIMARY KEY AUTO_INCREMENT,
    UnitName VARCHAR(100) NOT NULL
);

-- таблица Товаров
CREATE TABLE Product (
    ProductArticleNumber VARCHAR(100) PRIMARY KEY, 
    ProductName VARCHAR(255) NOT NULL,
    ProductDescription TEXT NOT NULL,
    ProductCategory INT NOT NULL,
    ProductPhoto VARCHAR(100),
    ProductManufacturer INT NOT NULL,
    ProductSupplier INT NOT NULL,
    ProductCost DECIMAL(19,4) NOT NULL,
    ProductDiscountAmount TINYINT NULL,
    ProductQuantityInStock INT NOT NULL,
    ProductMaxDiscount TINYINT NULL,
    ProductUnit INT NOT NULL,
    
    FOREIGN KEY (ProductCategory) REFERENCES ProductCategory(CategoryID),
    FOREIGN KEY (ProductManufacturer) REFERENCES ProductManufacturer(ManufacturerID),
    FOREIGN KEY (ProductSupplier) REFERENCES ProductSupplier(SupplierID),
    FOREIGN KEY (ProductUnit) REFERENCES UnitType(UnitID)
);

-- таблица Заказов
CREATE TABLE `Order` (
    OrderID INT PRIMARY KEY AUTO_INCREMENT,
    OrderStatus VARCHAR(50) NOT NULL,
    OrderDate DATETIME NOT NULL,
    OrderDeliveryDate DATETIME NOT NULL,
    OrderPickupPoint VARCHAR(255) NOT NULL,
    OrderClientName VARCHAR(255) NULL, -- Временное поле для импорта ФИО
    OrderCode INT NOT NULL,
    OrderUserID INT NULL, -- Ссылка на авторизованного пользователя
    
    FOREIGN KEY (OrderUserID) REFERENCES User(UserID)
);

-- таблица Состава заказа 
CREATE TABLE OrderProduct (
    OrderID INT NOT NULL,
    ProductArticleNumber VARCHAR(100) NOT NULL,
    ProductAmount INT NOT NULL,
    
    PRIMARY KEY (OrderID, ProductArticleNumber),
    FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID),
    FOREIGN KEY (ProductArticleNumber) REFERENCES Product(ProductArticleNumber)
);

-- заполнение ролей
INSERT INTO Role (RoleName) VALUES ('Администратор'), ('Менеджер'), ('Клиент');

-- заполнение справочников
INSERT INTO ProductCategory (CategoryName) 
SELECT DISTINCT `Категория товара` FROM import_product;

INSERT INTO ProductManufacturer (ManufacturerName) 
SELECT DISTINCT `Производитель` FROM import_product;

INSERT INTO ProductSupplier (SupplierName) 
SELECT DISTINCT `Поставщик` FROM import_product;

INSERT INTO UnitType (UnitName) 
SELECT DISTINCT `Единица измерения` FROM import_product;

-- импорт пользователей
INSERT INTO User (UserSurname, UserName, UserPatronymic, UserLogin, UserPassword, UserRole)
SELECT 
    SUBSTRING_INDEX(`ФИО`, ' ', 1),
    SUBSTRING_INDEX(SUBSTRING_INDEX(`ФИО`, ' ', 2), ' ', -1),
    SUBSTRING_INDEX(`ФИО`, ' ', -1),
    `Логин`,
    `Пароль`,
    (SELECT RoleID FROM Role WHERE RoleName = import_user.`Роль сотрудника`)
FROM import_user;

-- импорт товаров
INSERT INTO Product (
    ProductArticleNumber, ProductName, ProductDescription, ProductCategory, 
    ProductPhoto, ProductManufacturer, ProductSupplier, ProductCost, 
    ProductDiscountAmount, ProductQuantityInStock, ProductMaxDiscount, ProductUnit
)
SELECT 
    `Артикул`, 
    `Наименование`, 
    `Описание`, 
    (SELECT CategoryID FROM ProductCategory WHERE CategoryName = import_product.`Категория товара`),
    `Изображение`, 
    (SELECT ManufacturerID FROM ProductManufacturer WHERE ManufacturerName = import_product.`Производитель`),
    (SELECT SupplierID FROM ProductSupplier WHERE SupplierName = import_product.`Поставщик`),
    CAST(REPLACE(`Стоимость`, ',', '.') AS DECIMAL(19,4)), 
    `Действующая скидка`, 
    `Количество на складе`, 
    `Максимальная скидка`,
    (SELECT UnitID FROM UnitType WHERE UnitName = import_product.`Единица измерения`)
FROM import_product;

-- импорт заказов 
INSERT INTO `Order` (OrderID, OrderStatus, OrderDate, OrderDeliveryDate, OrderPickupPoint, OrderClientName, OrderCode)
SELECT 
    `Номер заказа`,
    `Статус заказа`,
    STR_TO_DATE(`Дата заказа`, '%d.%m.%Y'),
    STR_TO_DATE(`Дата доставки`, '%d.%m.%Y'),
    `Пункт выдачи`,
    `ФИО клиента`,
    `Код для получения`
FROM import_order;

-- привязка заказов к пользователям
UPDATE `Order` o
JOIN User u ON CONCAT(u.UserSurname, ' ', u.UserName, ' ', u.UserPatronymic) = o.OrderClientName
SET o.OrderUserID = u.UserID;

-- импорт состава заказа
INSERT INTO OrderProduct (OrderID, ProductArticleNumber, ProductAmount)
SELECT `Номер заказа`, `Артикул`, `Количество` 
FROM import_orderproduct;