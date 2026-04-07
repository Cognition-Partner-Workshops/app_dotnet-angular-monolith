const express = require('express');
const db = require('../database');
const { authenticateToken } = require('../middleware/auth');

const router = express.Router();

router.use(authenticateToken);

// Create order from cart
router.post('/', (req, res) => {
  try {
    const { shipping_name, shipping_address, shipping_city, shipping_state, shipping_zip, shipping_phone, payment_method = 'cod' } = req.body;

    if (!shipping_name || !shipping_address || !shipping_city || !shipping_state || !shipping_zip || !shipping_phone) {
      return res.status(400).json({ error: 'All shipping fields are required' });
    }

    // Get cart items
    const cartItems = db.prepare(`
      SELECT ci.*, p.name as product_name, p.price, p.stock
      FROM cart_items ci
      JOIN products p ON ci.product_id = p.id
      WHERE ci.user_id = ?
    `).all(req.user.id);

    if (cartItems.length === 0) {
      return res.status(400).json({ error: 'Cart is empty' });
    }

    // Validate stock
    for (const item of cartItems) {
      if (item.quantity > item.stock) {
        return res.status(400).json({ error: `Insufficient stock for ${item.product_name}` });
      }
    }

    const total = cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0);

    // Use a transaction for order creation
    const createOrder = db.transaction(() => {
      // Create order
      const orderResult = db.prepare(`
        INSERT INTO orders (user_id, total, shipping_name, shipping_address, shipping_city, shipping_state, shipping_zip, shipping_phone, payment_method, payment_status)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      `).run(
        req.user.id,
        Math.round(total * 100) / 100,
        shipping_name, shipping_address, shipping_city, shipping_state, shipping_zip, shipping_phone,
        payment_method,
        payment_method === 'cod' ? 'pending' : 'paid'
      );

      const orderId = orderResult.lastInsertRowid;

      // Create order items and update stock
      const insertItem = db.prepare('INSERT INTO order_items (order_id, product_id, product_name, quantity, price) VALUES (?, ?, ?, ?, ?)');
      const updateStock = db.prepare('UPDATE products SET stock = stock - ? WHERE id = ?');

      for (const item of cartItems) {
        insertItem.run(orderId, item.product_id, item.product_name, item.quantity, item.price);
        updateStock.run(item.quantity, item.product_id);
      }

      // Clear cart
      db.prepare('DELETE FROM cart_items WHERE user_id = ?').run(req.user.id);

      return orderId;
    });

    const orderId = createOrder();

    res.status(201).json({
      message: 'Order placed successfully',
      order_id: orderId
    });
  } catch (err) {
    console.error('Create order error:', err);
    res.status(500).json({ error: 'Failed to create order' });
  }
});

// Get user's orders
router.get('/', (req, res) => {
  try {
    const orders = db.prepare(`
      SELECT * FROM orders WHERE user_id = ? ORDER BY created_at DESC
    `).all(req.user.id);

    // Attach items to each order
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

// Get single order
router.get('/:id', (req, res) => {
  try {
    const order = db.prepare('SELECT * FROM orders WHERE id = ? AND user_id = ?').get(req.params.id, req.user.id);
    if (!order) {
      return res.status(404).json({ error: 'Order not found' });
    }

    const items = db.prepare('SELECT * FROM order_items WHERE order_id = ?').all(order.id);
    res.json({ ...order, items });
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch order' });
  }
});

// Cancel order (only if pending)
router.put('/:id/cancel', (req, res) => {
  try {
    const order = db.prepare('SELECT * FROM orders WHERE id = ? AND user_id = ?').get(req.params.id, req.user.id);

    if (!order) {
      return res.status(404).json({ error: 'Order not found' });
    }

    if (order.status !== 'pending') {
      return res.status(400).json({ error: 'Only pending orders can be cancelled' });
    }

    // Restore stock
    const items = db.prepare('SELECT * FROM order_items WHERE order_id = ?').all(order.id);
    const restoreStock = db.prepare('UPDATE products SET stock = stock + ? WHERE id = ?');

    db.transaction(() => {
      for (const item of items) {
        restoreStock.run(item.quantity, item.product_id);
      }
      db.prepare("UPDATE orders SET status = 'cancelled', updated_at = CURRENT_TIMESTAMP WHERE id = ?").run(order.id);
    })();

    res.json({ message: 'Order cancelled successfully' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to cancel order' });
  }
});

module.exports = router;
