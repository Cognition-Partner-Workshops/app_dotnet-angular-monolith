-- V1__initial_schema.sql
-- Initial schema migration matching the JPA entities and .NET EF Core models.
-- Column names use snake_case to match Spring Boot's default SpringPhysicalNamingStrategy.
-- Tables: customers, products, orders, order_items, inventory_items

CREATE TABLE IF NOT EXISTS customers (
    id         INTEGER PRIMARY KEY,
    address    VARCHAR(255),
    city       VARCHAR(255),
    email      VARCHAR(200) NOT NULL UNIQUE,
    name       VARCHAR(200) NOT NULL,
    phone      VARCHAR(255),
    state      VARCHAR(255),
    zip_code   VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS products (
    id          INTEGER PRIMARY KEY,
    category    VARCHAR(255),
    description VARCHAR(255),
    name        VARCHAR(200) NOT NULL,
    price       NUMERIC(18,2),
    sku         VARCHAR(50)  NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS orders (
    id               INTEGER PRIMARY KEY,
    customer_id      BIGINT,
    order_date       TIMESTAMP,
    shipping_address VARCHAR(255),
    status           VARCHAR(255),
    total_amount     NUMERIC(18,2),
    CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id         INTEGER PRIMARY KEY,
    order_id   BIGINT,
    product_id BIGINT,
    quantity   INTEGER NOT NULL,
    unit_price NUMERIC(18,2),
    CONSTRAINT fk_orderitems_order   FOREIGN KEY (order_id)   REFERENCES orders(id),
    CONSTRAINT fk_orderitems_product FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS inventory_items (
    id                 INTEGER PRIMARY KEY,
    product_id         BIGINT,
    quantity_on_hand   INTEGER NOT NULL,
    reorder_level      INTEGER NOT NULL,
    warehouse_location VARCHAR(255),
    last_restocked     TIMESTAMP,
    CONSTRAINT fk_inventory_product FOREIGN KEY (product_id) REFERENCES products(id)
);
