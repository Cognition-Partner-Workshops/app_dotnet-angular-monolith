-- V1__initial_schema.sql
-- Initial schema migration matching the JPA entities and .NET EF Core models.
-- Tables: Customers, Products, Orders, OrderItems, InventoryItems

CREATE TABLE IF NOT EXISTS Customers (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    name       VARCHAR(200) NOT NULL,
    email      VARCHAR(200) NOT NULL UNIQUE,
    phone      VARCHAR(255),
    address    VARCHAR(255),
    city       VARCHAR(255),
    state      VARCHAR(255),
    zipCode    VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS Products (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        VARCHAR(200) NOT NULL,
    description VARCHAR(255),
    category    VARCHAR(255),
    price       NUMERIC(18,2),
    sku         VARCHAR(50)  NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Orders (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    customer_id     BIGINT,
    orderDate       TIMESTAMP,
    status          VARCHAR(255),
    totalAmount     NUMERIC(18,2),
    shippingAddress VARCHAR(255),
    CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES Customers(id)
);

CREATE TABLE IF NOT EXISTS OrderItems (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id   BIGINT,
    product_id BIGINT,
    quantity   INTEGER NOT NULL,
    unitPrice  NUMERIC(18,2),
    CONSTRAINT fk_orderitems_order   FOREIGN KEY (order_id)   REFERENCES Orders(id),
    CONSTRAINT fk_orderitems_product FOREIGN KEY (product_id) REFERENCES Products(id)
);

CREATE TABLE IF NOT EXISTS InventoryItems (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id        BIGINT,
    quantityOnHand    INTEGER NOT NULL,
    reorderLevel      INTEGER NOT NULL,
    warehouseLocation VARCHAR(255),
    lastRestocked     TIMESTAMP,
    CONSTRAINT fk_inventory_product FOREIGN KEY (product_id) REFERENCES Products(id)
);
