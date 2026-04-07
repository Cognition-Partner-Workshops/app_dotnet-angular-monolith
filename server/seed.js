const db = require('./database');
const bcrypt = require('bcryptjs');

console.log('Seeding database...');

// Clear existing data
db.exec('DELETE FROM order_items');
db.exec('DELETE FROM orders');
db.exec('DELETE FROM cart_items');
db.exec('DELETE FROM products');
db.exec('DELETE FROM categories');
db.exec('DELETE FROM users');

// Reset auto-increment
db.exec("DELETE FROM sqlite_sequence WHERE name IN ('users','categories','products','orders','order_items','cart_items')");

// Create admin user
const adminPassword = bcrypt.hashSync('admin123', 10);
db.prepare('INSERT INTO users (name, email, password, role, phone) VALUES (?, ?, ?, ?, ?)')
  .run('Admin', 'admin@dairyfresh.com', adminPassword, 'admin', '+1234567890');

// Create demo customer
const customerPassword = bcrypt.hashSync('customer123', 10);
db.prepare('INSERT INTO users (name, email, password, role, phone, address) VALUES (?, ?, ?, ?, ?, ?)')
  .run('John Doe', 'john@example.com', customerPassword, 'customer', '+1987654321', '123 Main St, Springfield');

// Create categories
const categories = [
  { name: 'Fresh Milk', description: 'Farm-fresh milk and milk beverages', image_url: 'https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400' },
  { name: 'Cheese', description: 'Artisanal and everyday cheeses', image_url: 'https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?w=400' },
  { name: 'Butter & Ghee', description: 'Premium butter and clarified ghee', image_url: 'https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400' },
  { name: 'Yogurt & Curd', description: 'Probiotic yogurt and fresh curd', image_url: 'https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400' },
  { name: 'Cream & Paneer', description: 'Fresh cream and cottage cheese', image_url: 'https://images.unsplash.com/photo-1631452180519-c014fe946bc7?w=400' },
  { name: 'Ice Cream', description: 'Delicious dairy ice cream flavors', image_url: 'https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?w=400' },
];

const insertCategory = db.prepare('INSERT INTO categories (name, description, image_url) VALUES (?, ?, ?)');
for (const cat of categories) {
  insertCategory.run(cat.name, cat.description, cat.image_url);
}

// Create products
const products = [
  // Fresh Milk (category_id: 1)
  { name: 'Whole Milk (1L)', description: 'Fresh whole milk sourced from grass-fed cows. Rich in nutrients and calcium. Perfect for drinking, cooking, and baking.', price: 2.49, unit: 'bottle', stock: 100, category_id: 1, image_url: 'https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400' },
  { name: 'Skimmed Milk (1L)', description: 'Low-fat skimmed milk with all the goodness of dairy minus the extra fat. Great for health-conscious consumers.', price: 2.29, unit: 'bottle', stock: 80, category_id: 1, image_url: 'https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400' },
  { name: 'Toned Milk (500ml)', description: 'Toned milk with balanced fat content. Ideal for daily consumption and tea/coffee.', price: 1.49, unit: 'bottle', stock: 120, category_id: 1, image_url: 'https://images.unsplash.com/photo-1634141510639-d691d86f47be?w=400' },
  { name: 'Organic Milk (1L)', description: 'Certified organic milk from free-range cows. No antibiotics or hormones.', price: 4.99, unit: 'bottle', stock: 40, category_id: 1, image_url: 'https://images.unsplash.com/photo-1517448931760-9bf4414148c5?w=400' },
  { name: 'Chocolate Milk (500ml)', description: 'Creamy chocolate-flavored milk loved by kids and adults alike.', price: 2.99, unit: 'bottle', stock: 60, category_id: 1, image_url: 'https://images.unsplash.com/photo-1572443490709-e57652b64495?w=400' },

  // Cheese (category_id: 2)
  { name: 'Cheddar Cheese (200g)', description: 'Sharp and tangy aged cheddar cheese. Perfect for sandwiches, burgers, and cooking.', price: 5.49, unit: 'pack', stock: 50, category_id: 2, image_url: 'https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400' },
  { name: 'Mozzarella Cheese (200g)', description: 'Soft and stretchy mozzarella, ideal for pizza, pasta, and salads.', price: 4.99, unit: 'pack', stock: 60, category_id: 2, image_url: 'https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?w=400' },
  { name: 'Cottage Cheese (250g)', description: 'Fresh cottage cheese (paneer) made from pure milk. Great for curries and snacks.', price: 3.99, unit: 'pack', stock: 70, category_id: 2, image_url: 'https://images.unsplash.com/photo-1559561853-08451507cbe7?w=400' },
  { name: 'Cream Cheese (150g)', description: 'Smooth and spreadable cream cheese. Perfect for bagels, cheesecakes, and dips.', price: 3.49, unit: 'pack', stock: 45, category_id: 2, image_url: 'https://images.unsplash.com/photo-1528750997573-59b0d0aee7e7?w=400' },
  { name: 'Parmesan Cheese (100g)', description: 'Aged Italian-style parmesan with intense flavor. Grate over pasta and salads.', price: 6.99, unit: 'pack', stock: 30, category_id: 2, image_url: 'https://images.unsplash.com/photo-1590533468829-4df370771fb7?w=400' },

  // Butter & Ghee (category_id: 3)
  { name: 'Salted Butter (500g)', description: 'Creamy salted butter made from fresh cream. Ideal for spreading and cooking.', price: 4.49, unit: 'pack', stock: 80, category_id: 3, image_url: 'https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400' },
  { name: 'Unsalted Butter (500g)', description: 'Pure unsalted butter perfect for baking and gourmet cooking.', price: 4.79, unit: 'pack', stock: 60, category_id: 3, image_url: 'https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=400' },
  { name: 'Pure Ghee (500ml)', description: 'Traditional clarified butter (ghee) with rich aroma. A staple in Indian cooking.', price: 8.99, unit: 'jar', stock: 40, category_id: 3, image_url: 'https://images.unsplash.com/photo-1631452180519-c014fe946bc7?w=400' },
  { name: 'Organic Ghee (250ml)', description: 'Premium organic ghee made from grass-fed cow milk. Ayurvedic goodness.', price: 12.99, unit: 'jar', stock: 25, category_id: 3, image_url: 'https://images.unsplash.com/photo-1585672840563-f2af2ced763a?w=400' },

  // Yogurt & Curd (category_id: 4)
  { name: 'Natural Yogurt (500g)', description: 'Thick and creamy natural yogurt with live cultures. No added sugar.', price: 2.99, unit: 'cup', stock: 90, category_id: 4, image_url: 'https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400' },
  { name: 'Greek Yogurt (400g)', description: 'Extra thick Greek-style yogurt, high in protein and low in sugar.', price: 4.49, unit: 'cup', stock: 50, category_id: 4, image_url: 'https://images.unsplash.com/photo-1571212515416-fef01fc43637?w=400' },
  { name: 'Strawberry Yogurt (150g)', description: 'Delicious strawberry-flavored yogurt with real fruit pieces.', price: 1.79, unit: 'cup', stock: 100, category_id: 4, image_url: 'https://images.unsplash.com/photo-1505252585461-04db1eb84625?w=400' },
  { name: 'Fresh Curd (500g)', description: 'Homestyle fresh curd made from whole milk. Perfect for raita and lassi.', price: 1.99, unit: 'cup', stock: 110, category_id: 4, image_url: 'https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400' },
  { name: 'Probiotic Yogurt (200g)', description: 'Yogurt enriched with probiotics for gut health. Available in mixed fruit flavor.', price: 3.29, unit: 'cup', stock: 55, category_id: 4, image_url: 'https://images.unsplash.com/photo-1576179635662-9d1983e97e1e?w=400' },

  // Cream & Paneer (category_id: 5)
  { name: 'Fresh Cream (200ml)', description: 'Rich and smooth fresh cream for desserts, curries, and toppings.', price: 2.49, unit: 'pack', stock: 65, category_id: 5, image_url: 'https://images.unsplash.com/photo-1517093157656-b9eccef91cb1?w=400' },
  { name: 'Whipping Cream (250ml)', description: 'Heavy whipping cream ideal for cakes, pastries, and coffee toppings.', price: 3.99, unit: 'pack', stock: 40, category_id: 5, image_url: 'https://images.unsplash.com/photo-1464305795204-6f5bbfc7fb81?w=400' },
  { name: 'Paneer (250g)', description: 'Fresh and soft cottage cheese (paneer). Essential for Indian cuisine.', price: 3.49, unit: 'pack', stock: 80, category_id: 5, image_url: 'https://images.unsplash.com/photo-1631452180519-c014fe946bc7?w=400' },
  { name: 'Sour Cream (200ml)', description: 'Tangy sour cream perfect for baked potatoes, dips, and Mexican food.', price: 2.79, unit: 'pack', stock: 50, category_id: 5, image_url: 'https://images.unsplash.com/photo-1557142046-c704a3adf364?w=400' },

  // Ice Cream (category_id: 6)
  { name: 'Vanilla Ice Cream (500ml)', description: 'Classic vanilla bean ice cream made with real vanilla and fresh cream.', price: 5.99, unit: 'tub', stock: 45, category_id: 6, image_url: 'https://images.unsplash.com/photo-1570197571499-166b36435e9f?w=400' },
  { name: 'Chocolate Ice Cream (500ml)', description: 'Rich Belgian chocolate ice cream with a velvety smooth texture.', price: 6.49, unit: 'tub', stock: 40, category_id: 6, image_url: 'https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400' },
  { name: 'Strawberry Ice Cream (500ml)', description: 'Fresh strawberry ice cream with real fruit swirls. A summer favorite.', price: 5.99, unit: 'tub', stock: 35, category_id: 6, image_url: 'https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?w=400' },
  { name: 'Mango Ice Cream (500ml)', description: 'Tropical mango ice cream made from Alphonso mangoes and cream.', price: 6.99, unit: 'tub', stock: 30, category_id: 6, image_url: 'https://images.unsplash.com/photo-1501443762994-82bd5dace89a?w=400' },
];

const insertProduct = db.prepare('INSERT INTO products (name, description, price, unit, stock, category_id, image_url) VALUES (?, ?, ?, ?, ?, ?, ?)');

for (const product of products) {
  insertProduct.run(product.name, product.description, product.price, product.unit, product.stock, product.category_id, product.image_url);
}

console.log('Database seeded successfully!');
console.log(`- ${categories.length} categories created`);
console.log(`- ${products.length} products created`);
console.log('- Admin user: admin@dairyfresh.com / admin123');
console.log('- Demo customer: john@example.com / customer123');

process.exit(0);
