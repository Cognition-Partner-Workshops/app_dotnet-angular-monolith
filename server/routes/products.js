const express = require('express');
const db = require('../database');

const router = express.Router();

// Get all products (with optional filters)
router.get('/', (req, res) => {
  try {
    const { category, search, sort, limit, offset } = req.query;
    let query = `
      SELECT p.*, c.name as category_name 
      FROM products p 
      LEFT JOIN categories c ON p.category_id = c.id 
      WHERE p.is_active = 1
    `;
    const params = [];

    if (category) {
      query += ' AND p.category_id = ?';
      params.push(category);
    }

    if (search) {
      query += ' AND (p.name LIKE ? OR p.description LIKE ?)';
      params.push(`%${search}%`, `%${search}%`);
    }

    if (sort === 'price_asc') {
      query += ' ORDER BY p.price ASC';
    } else if (sort === 'price_desc') {
      query += ' ORDER BY p.price DESC';
    } else if (sort === 'name') {
      query += ' ORDER BY p.name ASC';
    } else {
      query += ' ORDER BY p.created_at DESC';
    }

    if (limit) {
      query += ' LIMIT ?';
      params.push(parseInt(limit));
      if (offset) {
        query += ' OFFSET ?';
        params.push(parseInt(offset));
      }
    }

    const products = db.prepare(query).all(...params);
    res.json(products);
  } catch (err) {
    console.error('Products error:', err);
    res.status(500).json({ error: 'Failed to fetch products' });
  }
});

// Get featured products
router.get('/featured', (req, res) => {
  try {
    const products = db.prepare(`
      SELECT p.*, c.name as category_name 
      FROM products p 
      LEFT JOIN categories c ON p.category_id = c.id 
      WHERE p.is_active = 1 AND p.stock > 0
      ORDER BY RANDOM() 
      LIMIT 8
    `).all();
    res.json(products);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch featured products' });
  }
});

// Get all categories
router.get('/categories', (req, res) => {
  try {
    const categories = db.prepare(`
      SELECT c.*, COUNT(p.id) as product_count 
      FROM categories c 
      LEFT JOIN products p ON c.id = p.category_id AND p.is_active = 1
      GROUP BY c.id
    `).all();
    res.json(categories);
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch categories' });
  }
});

// Get single product
router.get('/:id', (req, res) => {
  try {
    const product = db.prepare(`
      SELECT p.*, c.name as category_name 
      FROM products p 
      LEFT JOIN categories c ON p.category_id = c.id 
      WHERE p.id = ?
    `).get(req.params.id);

    if (!product) {
      return res.status(404).json({ error: 'Product not found' });
    }

    // Get related products from same category
    const related = db.prepare(`
      SELECT p.*, c.name as category_name 
      FROM products p 
      LEFT JOIN categories c ON p.category_id = c.id 
      WHERE p.category_id = ? AND p.id != ? AND p.is_active = 1
      LIMIT 4
    `).all(product.category_id, product.id);

    res.json({ ...product, related });
  } catch (err) {
    res.status(500).json({ error: 'Failed to fetch product' });
  }
});

module.exports = router;
