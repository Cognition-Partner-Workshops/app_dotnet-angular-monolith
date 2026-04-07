const express = require('express');
const db = require('../database');
const { authenticateToken, requireAdmin } = require('../middleware/auth');

const router = express.Router();

router.use(authenticateToken);
router.use(requireAdmin);

// Dashboard stats
router.get('/stats', (req, res) => {
  try {
    const totalOrders = db.prepare('SELECT COUNT(*) as count FROM orders').get().count;
    const totalRevenue = db.prepare('SELECT COALESCE(SUM(total), 0) as total FROM orders WHERE status != ?').get('cancelled').total;
    const totalProducts = db.prepare('SELECT COUNT(*) as count FROM products').get().count;
    const totalCustomers = db.prepare("SELECT COUNT(*) as count FROM users WHERE role = 'customer'").get().count;
    const pendingOrders = db.prepare("SELECT COUNT(*) as count FROM orders WHERE status = 'pending'").get().count;
    const lowStockProducts = db.prepare('SELECT COUNT(*) as count FROM products WHERE stock < 10 AND is_active = 1').get().count;

    const recentOrders = db.prepare(`
      SELECT o.*, u.name as customer_name, u.email as customer_email
      FROM orders o
      JOIN users u ON o.user_id = u.id
      ORDER BY o.created_at DESC
      LIMIT 10
    `).all();

    res.json({
      totalOrders,
      totalRevenue: Math.round(totalRevenue * 100) / 100,
      totalProducts,
      totalCustomers,
      pendingOrders,
      lowStockProducts,
      recentOrders
    });
  } catch (err) {
    console.error('Stats error:', err);
    res.status(500).json({ error: 'Failed to fetch stats' });
  }
});

// Get all products (including inactive)
router.get('/products', (req, res) => {
  try {
    const products = db.prepare(`
      SELECT p.*, c.name as category_name 
      FROM products p 
      LEFT JOIN categories c ON p.category_id = c.id
      ORDER BY p.created_at DESC
    `).all();
    res.json(products);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch products' });
  }
});

// Create product
router.post('/products', (req, res) => {
  try {
    const { name, description, price, unit, stock, category_id, image_url } = req.body;

    if (!name || !price) {
      return res.status(400).json({ error: 'Name and price are required' });
    }

    const result = db.prepare(
      'INSERT INTO products (name, description, price, unit, stock, category_id, image_url) VALUES (?, ?, ?, ?, ?, ?, ?)'
    ).run(name, description, price, unit || 'piece', stock || 0, category_id, image_url);

    res.status(201).json({ id: result.lastInsertRowid, message: 'Product created' });
  } catch (err) {
    console.error('Create product error:', err);
    res.status(500).json({ error: 'Failed to create product' });
  }
});

// Update product
router.put('/products/:id', (req, res) => {
  try {
    const { name, description, price, unit, stock, category_id, image_url, is_active } = req.body;

    db.prepare(`
      UPDATE products SET 
        name = COALESCE(?, name),
        description = COALESCE(?, description),
        price = COALESCE(?, price),
        unit = COALESCE(?, unit),
        stock = COALESCE(?, stock),
        category_id = COALESCE(?, category_id),
        image_url = COALESCE(?, image_url),
        is_active = COALESCE(?, is_active)
      WHERE id = ?
    `).run(name, description, price, unit, stock, category_id, image_url, is_active, req.params.id);

    res.json({ message: 'Product updated' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to update product' });
  }
});

// Delete product (soft delete)
router.delete('/products/:id', (req, res) => {
  try {
    db.prepare('UPDATE products SET is_active = 0 WHERE id = ?').run(req.params.id);
    res.json({ message: 'Product deactivated' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to delete product' });
  }
});

// Get all orders
router.get('/orders', (req, res) => {
  try {
    const { status } = req.query;
    let query = `
      SELECT o.*, u.name as customer_name, u.email as customer_email
      FROM orders o
      JOIN users u ON o.user_id = u.id
    `;
    const params = [];

    if (status) {
      query += ' WHERE o.status = ?';
      params.push(status);
    }

    query += ' ORDER BY o.created_at DESC';

    const orders = db.prepare(query).all(...params);

    const getItems = db.prepare('SELECT * FROM order_items WHERE order_id = ?');
    const ordersWithItems = orders.map(order => ({
      ...order,
      items: getItems.all(order.id)
    }));

    res.json(ordersWithItems);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch orders' });
  }
});

// Update order status
router.put('/orders/:id', (req, res) => {
  try {
    const { status, payment_status } = req.body;

    if (status) {
      db.prepare('UPDATE orders SET status = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?').run(status, req.params.id);
    }
    if (payment_status) {
      db.prepare('UPDATE orders SET payment_status = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?').run(payment_status, req.params.id);
    }

    res.json({ message: 'Order updated' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to update order' });
  }
});

// Manage categories
router.post('/categories', (req, res) => {
  try {
    const { name, description, image_url } = req.body;
    if (!name) {
      return res.status(400).json({ error: 'Category name is required' });
    }
    const result = db.prepare('INSERT INTO categories (name, description, image_url) VALUES (?, ?, ?)').run(name, description, image_url);
    res.status(201).json({ id: result.lastInsertRowid, message: 'Category created' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to create category' });
  }
});

router.put('/categories/:id', (req, res) => {
  try {
    const { name, description, image_url } = req.body;
    db.prepare('UPDATE categories SET name = COALESCE(?, name), description = COALESCE(?, description), image_url = COALESCE(?, image_url) WHERE id = ?')
      .run(name, description, image_url, req.params.id);
    res.json({ message: 'Category updated' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to update category' });
  }
});

router.delete('/categories/:id', (req, res) => {
  try {
    db.prepare('DELETE FROM categories WHERE id = ?').run(req.params.id);
    res.json({ message: 'Category deleted' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to delete category' });
  }
});

// Get all customers
router.get('/customers', (req, res) => {
  try {
    const customers = db.prepare(`
      SELECT u.id, u.name, u.email, u.phone, u.created_at, 
             COUNT(o.id) as order_count, 
             COALESCE(SUM(o.total), 0) as total_spent
      FROM users u
      LEFT JOIN orders o ON u.id = o.user_id AND o.status != 'cancelled'
      WHERE u.role = 'customer'
      GROUP BY u.id
      ORDER BY u.created_at DESC
    `).all();
    res.json(customers);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch customers' });
  }
});

module.exports = router;
